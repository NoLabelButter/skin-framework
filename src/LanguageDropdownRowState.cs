using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>Applies the generated selected-row face to each cloned TMP_Dropdown row.</summary>
    internal sealed class LanguageDropdownRowState : MonoBehaviour
    {
        private Toggle toggle;
        private Image selectedFace;
        private UnityAction<bool> changed;

        private void OnEnable()
        {
            toggle = GetComponent<Toggle>();
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == "SkinFrameworkSelected")
                {
                    selectedFace = images[i];
                    break;
                }
            }
            if (toggle == null || selectedFace == null)
                return;

            changed = SetSelected;
            toggle.onValueChanged.AddListener(changed);
            SetSelected(toggle.isOn);
        }

        private void OnDisable()
        {
            if (toggle != null && changed != null)
                toggle.onValueChanged.RemoveListener(changed);
            toggle = null;
            selectedFace = null;
            changed = null;
        }

        private void SetSelected(bool selected)
        {
            if (selectedFace != null)
                selectedFace.enabled = selected;
        }
    }
}
