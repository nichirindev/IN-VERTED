using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    public Transform rightHand;

    [Header("Swing Settings")]
    public float swingAngle = 120f;
    public float swingDuration = 0.3f;
    public float returnDuration = 0.2f;
    public Vector3 swingAxis = Vector3.right;

    [Header("Damage")]
    public float damage = 25f;
    public float knockbackForce = 8f;

    [HideInInspector] public bool isSwinging;

    private Coroutine swingCoroutine;

    private void Start()
    {
        if (rightHand == null)
            rightHand = FindBone("RightHand");

        if (rightHand == null)
            rightHand = FindBone("mixamorig:RightHand");

        if (rightHand != null)
            Debug.Log("EnemyAttack: Found hand bone - " + rightHand.name);
        else
            Debug.LogWarning("EnemyAttack: Could not find right hand bone!");
    }

    public void Swing()
    {
        if (isSwinging) return;
        swingCoroutine = StartCoroutine(SwingCoroutine());
    }

    private IEnumerator SwingCoroutine()
    {
        isSwinging = true;

        if (rightHand == null)
        {
            isSwinging = false;
            yield break;
        }

        Quaternion startRot = rightHand.localRotation;
        Quaternion endRot = startRot * Quaternion.AngleAxis(swingAngle, swingAxis);

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            t = t * t * (3f - 2f * t);
            rightHand.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            t = t * t * (3f - 2f * t);
            rightHand.localRotation = Quaternion.Slerp(endRot, startRot, t);
            yield return null;
        }

        rightHand.localRotation = startRot;
        isSwinging = false;
    }

    private Transform FindBone(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains(name))
                return child;
        }
        return null;
    }
}
