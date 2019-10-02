using mulova.commons;
using mulova.unicore;
using NUnit.Framework;
using UnityEngine;

public class IfActionTest : MonoBehaviour
{
    [If(null, IfAction.Error)]public GameObject obj;
    public int i;
    [If(0.1f, IfAction.Error)] public float f;

    [Test]
    public void IfAttributeTest()
    {
        FieldAttributeRegistry reg = new FieldAttributeRegistry(typeof(IfAttribute));
        reg.ForEach(this, (f, v) =>
        {
            var attr = f.GetAttribute<IfAttribute>();
            if (attr.action == IfAction.Error && attr.value == v)
            {
                Debug.LogError($"Error {name}.{f.Name} = {v}");
            }
        });
    }
}
