using System.Collections.Generic;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Provides an runtime IMGUI implementation of a drop-down menu,
    /// since Unity's drop-down menu implementations can only
    /// be used in Editor scripts.
    /// </summary>
    public static class GuiEx
    {
        /// <summary>
        /// Tracks state for a drop-down menu, e.g.
        /// the currently selected item.
        /// </summary>
        public struct DropDownState
        {
            /// <summary>
            /// The index of the currently selected item
            /// in the drop-down list.
            /// </summary>
            public int selectedIndex;
            /// <summary>
            /// True if the drop-down list is currently
            /// expanded, false otherwise.  The drop-down
            /// list is alternately expanded and closed
            /// by clicking on the drop-down button.
            /// </summary>
            public bool expanded;
        }

        /// <summary>
        /// Draw a drop-down menu for selecting among a list
        /// of items (strings).
        /// </summary>
        /// <param name="buttonRect">
        /// specifies the position and size of the
        /// button that is clicked to open the drop-down list
        /// </param>
        /// <param name="items">
        /// the list of items (strings) in the drop-down list
        /// </param>
        /// <param name="state">
        /// the current state of the drop-down list,
        /// i.e. the currently selected item and whether
        /// or not the drop-down list is expanded.
        /// </param>
        /// <param name="icon">
        /// expander icon (arrow) that is drawn
        /// on the right side of the drop-down button
        /// </param>
        /// <param name="buttonStyle">
        /// the GUIStyle for the button that opens/closes
        /// the drop-down list
        /// </param>
        /// <param name="listBackgroundStyle">
        /// The GUIStyle for the box that is drawn
        /// behind the drop-down list items.
        /// </param>
        /// <param name="listForegroundStyle">
        /// The GUIStyle for a box that is drawn
        /// on top of the drop-down list items.
        /// This style should be mostly transparent
        /// so that the individual list items
        /// are not hidden. I use this style to draw
        /// the border around the drop-down list
        /// after the individual list items have
        /// been rendered.
        /// </param>
        /// <param name="listItemStyle">
        /// the GUIStyle for the individual items
        /// in the drop-down list
        /// </param>
        /// <returns>
        /// the new state of the drop-down list
        /// </returns>
        public static DropDownState DropDownMenu(
            Rect buttonRect,
            List<string> items,
            DropDownState state,
            Texture2D icon,
            GUIStyle buttonStyle,
            GUIStyle listBackgroundStyle,
            GUIStyle listForegroundStyle,
            GUIStyle listItemStyle)
        {
            // draw drop-down button

            GUI.Label(buttonRect, items[state.selectedIndex], buttonStyle);

            // draw expander icon (arrow)

            var origColor = GUI.color;
            GUI.color = Color.black;

            const int iconMargin = 10;

            var iconAspect = (float)icon.width / icon.height;
            var iconHeight = buttonRect.height - 2 * iconMargin;
            var iconWidth = iconAspect * iconHeight;
            var iconRect = new Rect(
                buttonRect.x + buttonRect.width - iconMargin - iconWidth,
                buttonRect.y + iconMargin,
                iconWidth,
                iconHeight);

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            GUI.color = origColor;

            // handle mouse clicks on drop-down button

            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (buttonRect.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        state.expanded = !state.expanded;
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                        GUIUtility.hotControl = 0;
                    break;
            }

            // draw drop-down menu and handle mouse clicks

            if (state.expanded)
            {
                // calculate total height of drop-down list

                var listHeight = 0f;
                foreach (var item in items)
                    listHeight += listItemStyle.CalcSize(new GUIContent(item)).y;

                // Draw background for the list box.
                // Note that we draw the list box border separately
                // so that it does not get overwritten by the
                // individual list items.

                const float listOffset = 10;

                var listY = buttonRect.y - listOffset - listHeight;
                var listRect = new Rect(buttonRect.x, listY, buttonRect.width, listHeight);

                GUI.Box(listRect, "", listBackgroundStyle);

                // draw individual list items and handle mouse clicks

                var y = listY;

                for (var i = 0; i < items.Count; ++i)
                {
                    var itemHeight = listItemStyle.CalcSize(
                        new GUIContent(items[i])).y;

                    var itemRect = new Rect(buttonRect.x, y, buttonRect.width, itemHeight);

                    GUI.Box(itemRect, items[i], listItemStyle);

                    switch (Event.current.GetTypeForControl(controlID))
                    {
                        case EventType.MouseDown:
                            if (itemRect.Contains(Event.current.mousePosition))
                            {
                                state.selectedIndex = i;
                                state.expanded = false;
                            }
                            break;
                    }

                    y += itemHeight;
                }

                // Draw border around drop-down list.
                // This needs to be drawn last so that the individual list
                // items don't overwrite the border.

                GUI.Box(listRect, "", listForegroundStyle);
            }

            return state;
        }

    }
}