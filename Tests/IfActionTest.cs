using System.Collections;
using mulova.commons;
using mulova.unicore;
using UnityEngine;
using UnityEngine.TestTools;

public class IfActionTest : MonoBehaviour
{
    [If(null, IfAction.Error)]public GameObject obj;
    [NonNullableField] public GameObject nonNull;
    public int i;
    [If(0.1f, IfAction.Error)] public float f;

    [UnityTest]
    public IEnumerator IfAttributeTest()
    {
        GameObject go = new GameObject("IfActionTest");
        var test = go.AddComponent<IfActionTest>();
        yield return null;
        test.VerifyAttribute();
    }

    private void VerifyAttribute()
    {
        FieldAttributeRegistry<VerifyAttribute> reg = new FieldAttributeRegistry<VerifyAttribute>();
        reg.ForEach(this, (a, f, v) =>
        {
            if (!a.IsValid(this, f))
            {
                Debug.LogError($"Error {this.name}.{f.Name} = {v}");
            }
        });
    }


    private void Start()
    {
        VerifyAttribute();
    }
}
