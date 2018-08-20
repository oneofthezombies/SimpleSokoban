using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupController : MonoBehaviour
{
    public Transform[] GetElements()
    {
        List<Transform> transforms = new List<Transform>();
        GetComponentsInChildren(transforms);
        transforms.RemoveAt(0);
        return transforms.ToArray();
    }

    public void ClearElements()
    {
        foreach (var tf in GetElements())
        {
            Destroy(tf.gameObject);
        }
    }
}
