using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>Widget references for one active power-up timer row on the HUD.</summary>
    public class PowerRowUI
    {
        public PowerUpType Type;
        public RectTransform Root;
        public Image Icon;
        public UIFactory.ProgressBar Bar;
        public TextMeshProUGUI Time;
    }
}
