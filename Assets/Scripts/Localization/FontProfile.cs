using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Localization/Font Profile")]
public class FontProfile : ScriptableObject
{
    public TMP_FontAsset titleFont;
    public TMP_FontAsset bodyFont;
    public TMP_FontAsset uiFont;
    public TMP_FontAsset dialogueFont;
}
