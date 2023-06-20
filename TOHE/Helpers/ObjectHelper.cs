using UnityEngine;

namespace TOHE;

public static class ObjectHelper
{
    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this GameObject obj)
    {
        obj.ForEachChild((Il2CppSystem.Action<GameObject>)DestroyTranslator);
        var translator = obj.transform.GetComponentInChildren<TextTranslatorTMP>(true);
        if (translator != null) Object.Destroy(translator);
        translator = obj.GetComponent<TextTranslatorTMP>();
        if (translator != null) Object.Destroy(translator);
    }
    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this MonoBehaviour obj) => obj.gameObject.DestroyTranslator();
}
