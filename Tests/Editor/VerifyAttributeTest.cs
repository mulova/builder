using System.Collections;
using mulova.commons;
using mulova.unicore;
using UnityEngine;
using UnityEngine.TestTools;

public class VerifyAttributeTest
{
    [UnityTest]
    public IEnumerator IfAttributeTest()
    {
        var inst = new VerifyAttributeClass();
        yield return null;

        FieldAttributeRegistry<VerifyAttribute> reg = new FieldAttributeRegistry<VerifyAttribute>();
        reg.ForEach(this, (a, f, v) =>
        {
            if (f.Name.StartsWith("null", System.StringComparison.Ordinal))
            {
                if (a.IsValid(inst, f))
                {
                    Debug.LogError($"[{a.GetType().Name}]{f.Name}.{f.Name} = {v}");
                }
            }
            if (f.Name.StartsWith("nonNull", System.StringComparison.Ordinal))
            {
                if (!a.IsValid(inst, f))
                {
                    Debug.LogError($"[{a.GetType().Name}]{f.Name}.{f.Name} = {v}");
                }
            }
        });
    }
}

public class VerifyAttributeClass
{
    [If(null, IfAction.Error)] public GameObject null1;
    [NonNullableField] public GameObject null2;

    [If(null, IfAction.Error)] public Object nonNull1 = new Object();
    [NonNullableField] public Object nonNull2 = new Object();
}