using System;
using System.Collections.Generic;
using System.Text;
using MyBox;
using UnityEngine;

namespace PortableScanner
{
    public class PopupMessage : MonoBehaviour
    {
        public static PopupMessage instance;

        private string toastMessage;
        private float toastDuration;
        private float timer;

        private GUIStyle guiStyle;

        private void Awake()
        {
            // Ensure only one instance of Popup exists
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);

            // Initialize GUI style
            guiStyle = new GUIStyle();
            guiStyle.alignment = TextAnchor.MiddleCenter;
            guiStyle.fontSize = 20;
            guiStyle.normal.textColor = Color.white; // Set text color to white
            guiStyle.wordWrap = false; // Enable word wrapping
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(toastMessage))
            {
                Color boxColor = new Color(0f, 0f, 0f, 0.5f); // Black with 50% transparency

                // Set the GUI color to the defined color
                GUI.color = boxColor;

                // Calculate the height of each line based on the font size
                float lineHeight = guiStyle.fontSize + guiStyle.padding.vertical + 8;

                // Count the number of line breaks in the toastMessage
                int numLineBreaks = toastMessage.Split('\n').Length;

                // Calculate the total height of the box based on the number of lines
                float boxHeight = lineHeight * numLineBreaks;

                // Calculate the width of the text without automatic line breaking
                Vector2 textSize = guiStyle.CalcSize(new GUIContent(toastMessage));

                // Draw a box around the text
                float boxWidth = textSize.x + (10f * 2);

                // Draw a box around the text with padding on the left and right
                GUI.Box(new Rect(Screen.width / 2 - 148, 50, boxWidth, boxHeight), "");

                // Reset the GUI color to default
                GUI.color = Color.white;

                // Calculate the x position of the label to center it within the box
                float labelX = Screen.width / 2 - 148 + 10f; // Adding left padding

                // Draw the text inside the box without automatic line breaking
                GUI.Label(new Rect(labelX, 50, textSize.x, boxHeight), toastMessage, guiStyle);
            }
        }

        private void Update()
        {
            // Decrease timer and clear toast message when duration is over
            if (!string.IsNullOrEmpty(toastMessage))
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    toastMessage = "";
                }
            }
        }

        // Show a toast message
        public void ShowToast(string message, float duration = 2f)
        {
            toastMessage = message;
            toastDuration = duration;
            timer = duration;
        }
    }
}
