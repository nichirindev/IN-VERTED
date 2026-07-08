using DitzelGames.FastIK;
using UnityEngine;

public class HumanoidLegIK : MonoBehaviour
{
    public LayerMask whatIsGround;
    public float heightAboveGround;
    public FastIKFabric[] legs;
    private Transform[] legTargets;
    private Vector3[] targetPositions;
    private Vector3[] currentPositions;
    public Vector3 legTargetOffset;
    public Transform root;
    private float thresholdDistance;
    private float[] legProgress;
    private HumanoidBalance balance;
    public float legSpeed = 10f;
    private Vector3 currentVelocity;
    public float upAmount = 2f;

    private void Start()
    {
        balance = GetComponent<HumanoidBalance>();
        legTargets = new Transform[legs.Length];
        targetPositions = new Vector3[legs.Length];
        currentPositions = new Vector3[legs.Length];
        legProgress = new float[legs.Length];
        InitLegTargets();
        if ((double)heightAboveGround == 0.0)
        {
            heightAboveGround = CalcChainLength(legs[0]);
            thresholdDistance = heightAboveGround;
        }
        UpdateLegTargets();
        UpdateCurrentLegPosition(0);
        UpdateCurrentLegPosition(1);
        InvokeRepeating("SlowUpdate", 1f, 1f);
    }

    private void Update()
    {
        currentVelocity = balance.GetVelocity() * thresholdDistance;
        UpdateLegTargets();
        UpdateCurrentLegPositions(thresholdDistance);
        LerpLegs();
    }

    private void SlowUpdate() => UpdateCurrentLegPositions(thresholdDistance * 0.2f);

    private void InitLegTargets()
    {
        for (int index = 0; index < legs.Length; ++index)
        {
            int chainLength = legs[index].ChainLength;
            Transform transform = legs[index].transform;
            for (; chainLength > 0; --chainLength)
                transform = transform.parent;
            legTargets[index] = transform;
        }
    }

    private void UpdateLegTargets()
    {
        for (int index = 0; index < legTargets.Length; ++index)
        {
            Vector3 vector3 = legTargets[index].position - root.position;
            RaycastHit hitInfo;
            if (Physics.Raycast(legTargets[index].position + legTargetOffset.x * vector3 + currentVelocity + Vector3.up, Vector3.down, out hitInfo, 50f, (int)whatIsGround))
                targetPositions[index] = hitInfo.point;
        }
    }

    private void UpdateCurrentLegPositions(float threshold)
    {
        for (int leg = 0; leg < legs.Length && (OppositeLegGrounded(leg) || (double)legProgress[leg] >= 0.0099999997764825821 || (double)CheckDistanceFromTargetPoint(leg) >= 4.0); ++leg)
        {
            if ((double)CheckDistanceFromTargetPoint(leg) > (double)threshold)
                UpdateCurrentLegPosition(leg);
        }
    }

    private bool OppositeLegGrounded(int leg)
    {
        return (double)legProgress[(leg + 1) % legs.Length] < 0.0099999997764825821;
    }

    private float CheckDistanceFromTargetPoint(int leg)
    {
        return Vector3.Distance(currentPositions[leg], targetPositions[leg]);
    }

    private void UpdateCurrentLegPosition(int leg)
    {
        currentPositions[leg] = targetPositions[leg];
        legProgress[leg] = 1f;
    }

    private void LerpLegs()
    {
        Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
        for (int index = 0; index < legs.Length; ++index)
        {
            Transform target = legs[index].Target;
            legProgress[index] = Mathf.Lerp(legProgress[index], 0.0f, Time.deltaTime * legSpeed);
            Vector3 vector3 = Vector3.up * upAmount * legProgress[index];
            target.position = Vector3.Lerp(target.position, currentPositions[index] + vector3, Time.deltaTime * legSpeed);
            if (forward.sqrMagnitude > 0.001f)
                target.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private float CalcChainLength(FastIKFabric ik)
    {
        float length = 0f;
        Transform bone = ik.transform;
        for (int i = 0; i < ik.ChainLength && bone.parent != null; i++)
        {
            length += Vector3.Distance(bone.position, bone.parent.position);
            bone = bone.parent;
        }
        return length;
    }

    public void CollectGarbage()
    {
        foreach (FastIKFabric leg in legs)
            Object.Destroy((Object)leg.Target.gameObject);
    }

    public void ForceCurrentPosition(int i)
    {
        if (legProgress == null)
            return;
        legProgress[i] = 1f;
        legs[i].Target.position = legs[i].transform.position;
    }
}
