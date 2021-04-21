using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Promotion is a special case of <see cref="MoveBifurcation"/>.
    /// </summary>
    public class PromoteNode : TopologyAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="promoting"></param>
        public PromoteNode(BranchNode promoting)
        {
            promoted = promoting;
            action = Make();
            demoted = action?.B.End;
        }

        private readonly BranchNode promoted;
        private readonly BranchNode demoted;
        private readonly MoveBifurcation action;
        private bool executed = false;

        private MoveBifurcation Make()
        {
            // Promoting a branch: the branch to be promoted is raised one level to the root.
            // To do this, move the sibling branch to bifurcate from the current parent's sibling
            // i.e. (1, (2, 3)) -> ((1, 2), 3) is a promotion of branch 3, and a demotion of 1.
            // Returns null if not possible.
            var promoting = promoted.Upstream;
            if (promoting == null ||
                promoting.Parent is not Branch parent ||
                parent.End is not Bifurcation end ||
                parent.Start is not Bifurcation start)
            {
                return null;
            }
            var moving = end.Downstream[1 - end.IndexOf(promoting)];
            var target = start.Downstream[1 - start.IndexOf(parent)];
            return new MoveBifurcation(moving, target);
        }

        /// <summary>
        /// Only access this after the forwards action has been executed.
        /// </summary>
        public PromoteNode Inverse =>
            // Promotion was achieved by moving 2 -> 1, so now we undo this by promoting 1,
            // which will move 2 -> 3. Only access after making move though, as we create a
            // bifurcation move request which needs the structure created by execution.
            executed ? new PromoteNode(demoted) : null;

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            if (!executed)
            {
                action.Execute(propagateLogical, propagatePhysical);
                executed = true;
            }
        }

        /// <inheritdoc/>
        public override bool IsPermissible()
        {
            return action != null && !executed;
        }
    }
}
