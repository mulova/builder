using mulova.unicore;
using UnityEngine;

public class IfActionTest : MonoBehaviour
{
    [If(null, IfAction.Error)]public GameObject obj;
    public int i;
    [If(0.1f, IfAction.Error)] public float f;
}
