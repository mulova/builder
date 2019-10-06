using mulova.commons;
using mulova.unicore;
using UnityEngine;

public class IfAttributeBehaviour : MonoBehaviour
{
    [If(null, IfAction.Error)] public GameObject nullErr;
    [NonNullableField] public GameObject nonNull;
    public int i;
    [If(0.1f, IfAction.Error)] public float f;

    public void VerifyAttribute()
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
}
