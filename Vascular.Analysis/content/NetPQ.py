# Vascular Analysis: NetPQ
# Andrew Guy
#
# Solves for node pressures given an arbitrary network matrix and a specified
# set of fixed node pressures/net outward flow rates.
#
# Takes N nodes, indexed by i, j; and M branches, indexed by k.
# Creates a resistance matrix by taking in a diagonal pipe resistance matrix as
# (k, R) and a connection matrix as (i, j).
# Takes a vector of known net flow rates at nodes as (i, Q) and known pressures
# as (i, P), ensuring that each node appears exactly once in either of the two
# sets and that the pressure set is not empty (otherwise the all 1 vector in the
# null space of Ap = Q makes the problem underdetermined).
#
# Can be used as a module, and invoked directly using the NetPQ class, or
# attached to streams using the listener class. Can also be run as a program, in
# which case it attaches itself to stdin, stdout and stderr. See listener for
# interoperation instructions.
# Required dependencies: numpy
# Optional dependencies: scipy (when using sparse or solver options)
# Flags when run as main:
#       --sparse, -s        Use sparse matrices to store connection matrix,
#                           defaults to using dok_matrix as the construction
#                           format. Intermediate formats resulting from
#                           operations e.g. A @ B are implementation defined.
#                           If not specified, uses dense matrices with np.zeros
#                           and np.linalg.solve.
#       --solver, -S        Use a specified solver: if sparse, imported from
#                           scipy.sparse.linalg - will be modified to return
#                           first from tuple. If not specified, uses spsolve.
#                           If not sparse, acts as a flag to use the scipy
#                           solve method.
#       --densify, -d       Use sparse matrices for storage, then convert to
#                           dense for solving
#       --verbose, -v       Writes storage and solving options, as well as
#                           when exiting. For checking that it's working.
#
# Mind your Ps and Qs

import numpy as np
import numpy.linalg as npla
import struct

class NetPQ:
    def __init__(self, N, M, n = None, s = None, e = None):
        self.n = n if n is not None else lambda d: np.zeros(d)
        self.s = s if s is not None else lambda A, b: npla.solve(A, b)
        self.e = e if e is not None else npla.LinAlgError
        self.N = N
        self.M = M
        self.p = np.zeros(N)
        self.q = None
        self.B = self.n((M, N))
        self.R = np.zeros(M)
        self.P = dict()
        self.Q = dict()
    
    def addBranch(self, i, j, k, r):
        self.B[k, i] = 1.0
        self.B[k, j] = -1.0
        self.R[k] = r
    
    def setFlow(self, i, q):
        self.Q[i] = q
    
    def setPressure(self, i, p):
        self.P[i] = p

    def setDefault(self):
        for i in range(0, self.N):
            if (i not in self.P) and (i not in self.Q):
                self.Q[i] = 0.0
    
    def verify(self):
        kp = self.P.keys()
        if len(kp) == 0:
            return False, "No pressure datum specified"
        kq = self.Q.keys()
        ki = kp & kq
        if len(ki) != 0: 
            return False, (
                "Intersection of fixed pressure and fixed net outward flow "
                f"rate nodes is non-empty: {ki}")
        ku = kp | kq
        if len(ku) != self.N:
            return False, (
                "Union of fixed pressure and fixed net outward flow rate nodes "
                "does not contain all nodes: missing "
                f"{set(range(0, self.N)) - ku}")    
        for k in range(0, self.M):
            nz = np.nonzero(self.B[k, :])
            if len(nz) != 2:
                return False, (
                    f"Connection matrix row {k} does not have 2 entries: "
                    f"{self.B[k, :]}")
            x = self.B[k, nz[0]]
            y = self.B[k, nz[1]]
            if not((x == -1.0 and y == 1.0) or (x == 1.0 and y == -1.0)):
                return False, (
                    f"Connection matrix row {k} entries are not +/- 1 pair: "
                    f"{x}, {y}")      
        if np.count_nonzero(self.R) != self.M:
            return False, (
                "Resistance vector contains zero elements: indices "
                f"{np.nonzero(self.R == 0.0)}")      
        return True, ""

    def solveSafe(self, q = False):
        try:
            self.solve(q)
            return True, ""
        except ValueError as ve:
            return False, f"ValueError in 'solve': '{str(ve)}'"
        except self.e as lae:
            return False, f"LinAlgError in 'solve': '{str(lae)}'"
        except BaseException as e:
            return False, f"Unexpected exception in 'solve': {repr(e)}"
    
    def solve(self, q = False):
        # Get submatrices and subvectors split by fixed flow, pressure
        # We solve: Xp + YP = Q
        FQ = list(self.Q.keys())
        FP = list(self.P.keys())
        Bq = self.n((self.M, len(FQ)))
        Bp = self.n((self.M, len(FP)))
        Q = np.zeros(len(FQ))
        P = np.zeros(len(FP))
        for i in range(0, len(FQ)):
            n = FQ[i]
            Q[i] = self.Q[n]
            Bq[:, i] = self.B[:, n]
        for i in range(0, len(FP)):
            n = FP[i]
            P[i] = self.P[n]
            Bp[:, i] = self.B[:, n] 
        BqR = Bq.transpose() @ np.diag(1.0 / self.R)
        X = BqR @ Bq
        # Y = BqR * Bp, but better to multiply vector twice?
        QYP = Q - BqR @ (Bp @ P)
        p = self.s(X, QYP)
        # Now rearrange indices - p has been ordered according to previous key
        # order
        for i in range(0, len(FQ)):
            n = FQ[i]
            self.p[n] = p[i]
        for i in range(0, len(FP)):
            n = FP[i]
            self.p[n] = P[i]
        # Do we want to solve for flow as well?
        if q:
            self.q = np.diag(1.0 / self.R) @ (self.B @ self.p)

class Listener:
    def __init__(self, istrm, ostrm, estrm,
                 n = None, s = None, e = None):
        self.n = n
        self.s = s
        self.e = e
        self.I = istrm
        self.O = ostrm
        self.E = estrm

    def makeNew(self, n, m):
        self.N = NetPQ(n, m, self.n, self.s, self.e)

# Interoperating with this: instructions sent as int32 followed by arguments.
# All data sent in little-endian format.
# For methods which may indicate success, errors are written to estrm as a line
# and immediately flushed - not that this necessarily helps beyond reporting.
# 1     n: int32        Create a new blank network with n nodes and m branches
#       m: int32
# 2     i: int32        Set branch k to link nodes i -> j with resistance r
#       j: int32
#       k: int32
#       r: float64
# 3     i: int32        Set node i to have fixed pressure p
#       p: float64
# 4     i: int32        Set node i to have fixed net outwards flow rate q
#       q: float64
# 5                     Default, sets all non-assigned nodes to 0 flow
# 6                     Verify, write 1/0 to output as int32
# 7                     Solve, write 1/0 to output as int32 then output as
#                       vector of N pressures as float64 if 1
# 8                     Clear, wipe reference to NetPQ class for collection
# 0                     Quit
class BinaryListener(Listener):
    def __init__(self, istrm, ostrm, estrm,
                 n = None, s = None, e = None):
        super().__init__(istrm, ostrm, estrm, n, s, e)
        self.A = {
            1: self.new,
            2: self.branch,
            3: self.pressure,
            4: self.flow,
            5: self.default,
            6: self.verify,
            7: self.solve,
            8: self.clear
        }
    
    # Binary interop utilities - '<' specifies little-endian
    def getInt32(self):
        return struct.unpack('<i', self.I.read(4))[0]
    
    def getFloat64(self):
        return struct.unpack('<d', self.I.read(8))[0]
    
    def putInt32(self, i):
        self.O.write(struct.pack('<i', i))
    
    def putFloat64(self, f):
        self.O.write(struct.pack('<d', f))

    # Main loop
    def listen(self):
        try:
            while True:
                c = self.getInt32()
                if c == 0:
                    return
                a = self.A.get(c, None)
                if a is not None:
                    a()
                else:
                    self.E.write(f"Invalid command: {c}\n")
                    return
        except struct.error:
            self.E.write("Structure unpacking error: it is likely that "
                         "reading from input stream has returned an EOF, "
                         "indicating a premature closing of the stream\n")
        except BaseException as e:
            self.E.write(f"Unexpected exception: {repr(e)}\n")

    # Actions, thin wrapper around the NetPQ class to read/write data
    # to/from streams
    def new(self):
        n = self.getInt32()
        m = self.getInt32()
        self.makeNew(n, m)
    
    def branch(self):
        i = self.getInt32()
        j = self.getInt32()
        k = self.getInt32()
        r = self.getFloat64()
        self.N.addBranch(i, j, k, r)
    
    def pressure(self):
        i = self.getInt32()
        p = self.getFloat64()
        self.N.setPressure(i, p)
    
    def flow(self):
        i = self.getInt32()
        q = self.getFloat64()
        self.N.setFlow(i, q)
    
    def verify(self):
        b, r = self.N.verify()
        self.putInt32(1 if b else 0)
        self.O.flush()
        if not b:
            self.writeReason(r)
    
    def solve(self):
        b, r = self.N.solveSafe()
        self.putInt32(1 if b else 0)
        if b:
            for p in self.N.p:
                self.putFloat64(p)
        else:
            self.writeReason(r)
        self.O.flush()

    def writeReason(self, r):
        self.E.write(r)
        self.E.write('\n')
        self.E.flush()

    def clear(self):
        self.N = None

    def default(self):
        self.N.setDefault()

def translate(args, err):
    fnew = None
    fsolve = None
    etype = None
    log = err.write if args.verbose else lambda s: None
    if args.sparse:
        log("Using sparse storage\n")
        import scipy.sparse as sps
        fnew = lambda d: sps.dok_matrix(d)
        if not args.densify:
            import scipy.sparse.linalg as spsla
            if args.solver:
                try:
                    solver = getattr(spsla, args.solver)
                except AttributeError as ae:
                    err.write(f"Could not find sparse solver '{args.solver}'\n")
                    ae.handled = True
                    raise
                fsolve = lambda A, b: solver(A, b)[0]
                log(f"Using sparse solver '{args.solver}'\n")
            else:
                fsolve = lambda A, b: spsla.spsolve(A, b)
                log(f"Using default sparse solver\n")
    else:
        log("Using dense storage\n")
    if args.solver and (not args.sparse or args.densify):
        log("Using scipy dense solver\n")
        import scipy.linalg as spla
        fsolve = lambda A, b: spla.solve(A, b, assume_a = 'pos',
                                         overwrite_a = True, overwrite_b = True)
        etype = spla.LinAlgError
    if fsolve is None:
        log("Using numpy dense solver\n")
    return fnew, fsolve, etype, log

def main():
    # Build argument parser and parse
    import argparse as ap
    parser = ap.ArgumentParser()
    parser.add_argument('--sparse', '-s', action = 'store_true')
    parser.add_argument('--solver', '-S', nargs = '?', const = True,
                        default = False)
    parser.add_argument('--densify', '-d', action = 'store_true')
    parser.add_argument('--verbose', '-v', action = 'store_true')
    args = parser.parse_args()
    # Construct delegate methods from options
    fnew, fsolve, etype, log = translate(args, sys.stderr)
    # Now attach to std streams and execute
    state = BinaryListener(sys.stdin.buffer, sys.stdout.buffer, sys.stderr,
                           fnew, fsolve, etype)
    state.listen()
    log("Exited")

if __name__ == "__main__":
    import sys
    try:
        main()
    except BaseException as e:
        if not hasattr(e, 'handled'):
            sys.stderr.write(f"Unhandled exception: {repr(e)}")
        sys.exit(1)
