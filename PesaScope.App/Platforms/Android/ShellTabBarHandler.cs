using Android.Content.Res;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace PesaScope.App.Platforms.Android;

public class PesaLensShellHandler : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
    {
        return new PesaLensBottomNavTracker();
    }
}

public class PesaLensBottomNavTracker : IShellBottomNavViewAppearanceTracker
{
    public void ResetAppearance(BottomNavigationView bottomView)
    {
        ApplyColors(bottomView);
    }

    public void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        ApplyColors(bottomView);
    }

    public void Dispose() { }

    // Ends up not accounting for icon and text colors on switching to light mode
    //private static void ApplyColors(BottomNavigationView bottomView)
    //{
    //    // Resolve dark mode
    //    var uiMode = bottomView.Context?.Resources?.Configuration?.UiMode & UiMode.NightMask;
    //    bool isDark = uiMode == UiMode.NightYes;

    //    var bgColor = isDark
    //        ? global::Android.Graphics.Color.ParseColor("#111F19")
    //        : global::Android.Graphics.Color.ParseColor("#F7FAF8");

    //    bottomView.SetBackgroundColor(bgColor);
    //}

    private static void ApplyColors(BottomNavigationView bottomView)
    {
        var uiMode = bottomView.Context?.Resources?.Configuration?.UiMode & UiMode.NightMask;
        bool isDark = uiMode == UiMode.NightYes;

        // Background
        var bgColor = isDark
            ? global::Android.Graphics.Color.ParseColor("#111F19")   // SurfaceDark
            : global::Android.Graphics.Color.ParseColor("#F7FAF8");  // Surface
        bottomView.SetBackgroundColor(bgColor);

        // Selected item color (active tab icon + label)
        var selectedColor = isDark
            ? global::Android.Graphics.Color.ParseColor("#5ECBA1")   // PrimaryDark
            : global::Android.Graphics.Color.ParseColor("#1A8C62");  // Primary

        // Unselected item color (inactive tab icons + labels)
        var unselectedColor = isDark
            ? global::Android.Graphics.Color.ParseColor("#B3C8C0")   // OnSurfaceVariantDark
            : global::Android.Graphics.Color.ParseColor("#4B5F56");  // OnSurfaceVariant

        // Build a ColorStateList that Android's BottomNavigationView understands
        var states = new int[][]
        {
            new[] {  global::Android.Resource.Attribute.StateChecked },   // selected
            new[] {  -global::Android.Resource.Attribute.StateChecked },   // unselected (Negative -> the attribute must NOT be active for this row to match)
        };

        var colors = new int[]
        {
            selectedColor,
            unselectedColor,
        };

        var colorStateList = new ColorStateList(states, colors);

        // Apply to both icons and text
        bottomView.ItemIconTintList = colorStateList;
        bottomView.ItemTextColor = colorStateList;
    }
}