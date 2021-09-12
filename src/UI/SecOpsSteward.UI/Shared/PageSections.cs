using Microsoft.AspNetCore.Components;

namespace SecOpsSteward.UI.Shared
{
    public class PageSections : ComponentBase
    {
        [CascadingParameter]
        public DynamicMainLayout Layout { get; set; }

        [Parameter]
        public RenderFragment TitleBar { get; set; }

        [Parameter]
        public RenderFragment ButtonBar { get; set; }

        [Parameter]
        public RenderFragment SecondaryDrawer { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void OnInitialized()
        {
            Layout.SetDynamicLayout(TitleBar, ButtonBar, SecondaryDrawer, Body);
        }

        public void LayoutStateHasChanged()
        {
            Layout.LayoutStateHasChanged();
        }
    }
}
