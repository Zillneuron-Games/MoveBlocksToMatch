using System;

namespace Zillneuron.UILayout
{
    public class GridListFragmentView : AListFragmentView
    {
        protected override bool ListEndReached { get { return viewPosition.y < 0.0f || viewPosition.x > 1.0f; } }
    }
}

