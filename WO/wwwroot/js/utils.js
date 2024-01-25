function GetRadWindow() {
    /// <summary>
    /// Gets the telerik window.
    /// </summary>
    /// <returns type="">The telerik window control.</returns>
    var oWindow = null;
    if (window.radWindow) oWindow = window.radWindow; //Will work in Moz in all cases, including clasic dialog
    else if (window.frameElement.radWindow) oWindow = window.frameElement.radWindow; //IE (and Moz as well)

    return oWindow;
}