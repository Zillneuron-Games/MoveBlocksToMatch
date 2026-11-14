using System;

namespace Zillneuron.UILayout
{
    public class VerticalListFragmentView : AListFragmentView
    {
        protected override bool ListEndReached { get { return viewPosition.y < 0.0f; } }

    }
}
