using UnityEngine;

[RequireComponent(typeof(HumanoidLegIK))]
public class HumanoidBalance : MonoBehaviour
{
    public enum BalanceState
    {
        active,
        tumbling,
        falling,
        recovering,
        dead
    }

    [Header("Body References")]
    public Transform root;
    public Transform head;
    public Transform torso;

    [Header("Balance Tuning")]
    public float legPushForce = 0.55f;
    public float groundCheckRadius = 0.2f;
    public float moveLegsWithSpeedScale = 0.25f;
    public float moveSpeed = 10f;
    public float maxRotationForce = 0.05f;
    public float recoverTime = 2f;
    public float recoveryForce = 0.3f;
    public float tumbleAngle = 30f;
    public float fallAngle = 70f;
    public float getupMagT = 0.2f;
    public float getupAng = 15f;

    [HideInInspector] public BalanceState state = BalanceState.active; // BalanceState.recovering; for falling at starting
    [HideInInspector] public HumanoidLegIK ik;
    [HideInInspector] public bool recovering;

    private Rigidbody rb;
    private Rigidbody headRb;
    private Rigidbody torsoRb;
    private float force;
    private float rotationForce = 0.01f;
    private float stabilizeForce = 1f;
    private bool ragdoll;
    private int nLegs;
    private Transform[] groundChecks;

    private void Start()
    {
        rb = root.GetComponent<Rigidbody>();
        headRb = head != null ? head.GetComponent<Rigidbody>() : rb;
        torsoRb = torso != null ? torso.GetComponent<Rigidbody>() : rb;
        ik = GetComponent<HumanoidLegIK>();
        CalculateForce();
        UpdateState(BalanceState.active);
        nLegs = ik.legs.Length;
        groundChecks = new Transform[nLegs];
        for (int index = 0; index < nLegs; ++index)
            groundChecks[index] = ik.legs[index].transform;
        DisableSelfCollision(true);
    }

    private void FixedUpdate()
    {
        if (state == BalanceState.dead)
            return;

        bool minOneGrounded = false;
        for (int index = 0; index < nLegs; ++index)
        {
            if (Physics.CheckSphere(groundChecks[index].position, groundCheckRadius, (int)ik.whatIsGround))
                minOneGrounded = true;
        }

        float groundDist = 0.0f;
        if (state == BalanceState.active || state == BalanceState.tumbling || state == BalanceState.recovering || state == BalanceState.falling)
        {
            RaycastHit hitInfo;
            if (!Physics.Raycast(root.position, Vector3.down, out hitInfo, ik.heightAboveGround * 3f, (int)ik.whatIsGround))
                UpdateState(BalanceState.falling);
            else
                groundDist = hitInfo.distance;
        }

        float tiltAngle = Vector3.Angle(Vector3.up, root.up);

        if (state == BalanceState.falling)
        {
            if ((double)groundDist != 0.0 && (double)groundDist < (double)ik.heightAboveGround * 1.5 && (double)tiltAngle < 50.0)
            {
                UpdateState(BalanceState.active);
                CancelInvoke("GetUp");
                ConfigureLegs(false);
                recovering = false;
            }
            else
            {
                if (IsInvoking("GetUp"))
                    return;
                Invoke("GetUp", recoverTime);
            }
        }
        else if (state == BalanceState.recovering)
        {
            bool nearGround = Physics.CheckSphere(root.position, 0.5f, (int)ik.whatIsGround);
            if ((double)groundDist < (double)ik.heightAboveGround | nearGround)
            {
                headRb.AddForce(Vector3.up * force * recoveryForce * 1.1f);
                rb.AddForce(Vector3.up * force * recoveryForce * 0.9f);
            }
            if (((double)tiltAngle >= (double)getupAng || (double)torsoRb.velocity.magnitude >= (double)getupMagT) && ((double)groundDist <= (double)ik.heightAboveGround * 0.85 || (double)groundDist >= (double)ik.heightAboveGround * 1.85 || (double)tiltAngle >= 30.0))
                return;
            UpdateState(BalanceState.active);
            CancelInvoke("RecoveryCooldown");
            Invoke("RecoveryCooldown", 2f);
        }
        else if (state == BalanceState.active && (double)rb.velocity.magnitude < 1.0 && (double)groundDist > (double)ik.heightAboveGround && (double)groundDist < (double)ik.heightAboveGround + (double)ik.heightAboveGround * 0.1)
        {
            headRb.AddForce(Vector3.up * force * 0.86f);
        }
        else
        {
            float heightRatio = Mathf.Clamp((float)(1.0 - (double)RootHeight() / (double)ik.heightAboveGround), -1f, 1f);
            if ((double)tiltAngle < (double)tumbleAngle)
                UpdateState(BalanceState.active);
            else if ((double)tiltAngle < (double)fallAngle)
                UpdateState(BalanceState.tumbling);
            else if ((double)tiltAngle > (double)fallAngle)
                UpdateState(BalanceState.falling);

            if (minOneGrounded)
            {
                rb.AddForce(root.up * force * heightRatio * 2f);
                rb.AddForce(root.up * force * legPushForce);
            }
            if ((double)groundDist >= (double)ik.heightAboveGround * 2.0)
                return;
            StabilizingBody();
        }
    }

    private void RecoveryCooldown() => recovering = false;

    public void Concuss()
    {
        UpdateState(BalanceState.falling);
        ConfigureLegs(true);
        recovering = true;
        Invoke("GetUp", recoverTime * Random.Range(0.7f, 1.5f));
    }

    private void GetUp()
    {
        if (Physics.CheckSphere(root.position, ik.heightAboveGround * 0.5f, (int)ik.whatIsGround))
        {
            UpdateState(BalanceState.recovering);
            ConfigureLegs(false);
        }
        else
            Invoke(nameof(GetUp), recoverTime);
    }

    private void ConfigureLegs(bool makeRagdoll)
    {
        if (makeRagdoll == ragdoll)
            return;
        ragdoll = makeRagdoll;
        for (int i = 0; i < ik.legs.Length; ++i)
        {
            int chainLength = ik.legs[i].ChainLength;
            Transform transform = ik.legs[i].transform;
            for (; chainLength > 0; --chainLength)
            {
                transform = transform.parent;
                if (makeRagdoll)
                    transform.gameObject.AddComponent<CharacterJoint>().connectedBody = transform.parent.GetComponent<Rigidbody>();
                else
                    Object.Destroy(transform.gameObject.GetComponent<Joint>());
            }
            foreach (Rigidbody rb in transform.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = !makeRagdoll;
                rb.interpolation = makeRagdoll ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }
            ik.legs[i].enabled = !makeRagdoll;
            ik.ForceCurrentPosition(i);
        }
    }

    private float RootHeight()
    {
        RaycastHit hitInfo;
        return Physics.Raycast(root.position, Vector3.down, out hitInfo, 10f, (int)ik.whatIsGround) ? hitInfo.distance : 0.0f;
    }

    private void StabilizingBody()
    {
        headRb.AddForce(Vector3.up * force * stabilizeForce);
        torsoRb.AddForce(Vector3.down * force * stabilizeForce);
    }

    private void CalculateForce()
    {
        float totalMass = 0.0f;
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (!rb.isKinematic)
                totalMass += rb.mass;
        }
        force = totalMass * -Physics.gravity.y;
    }

    public void RotateBody(Vector3 dir)
    {
        rb.AddTorque(Vector3.up * Mathf.Clamp(Mathf.DeltaAngle(root.transform.eulerAngles.y, Quaternion.LookRotation(dir).eulerAngles.y), -2f, 2f) * force * rotationForce);
    }

    public void MoveBody(Vector3 dir)
    {
        rb.AddForce(dir * moveSpeed * rb.mass);
        headRb.AddForce(dir * moveSpeed * headRb.mass);
        torsoRb.AddForce(dir * moveSpeed * torsoRb.mass);
    }

    public void UpdateState(BalanceState s)
    {
        if (state == s)
            return;
        state = s;
        switch (s)
        {
            case BalanceState.active:
                ConfigureRb(5f, 5f, maxRotationForce, 1f);
                break;
            case BalanceState.tumbling:
                ConfigureRb(1f, 4f, 0.0f, 0.1f);
                break;
            case BalanceState.falling:
                ConfigureRb(0.0f, 0.0f, 0.0f, 0.0f);
                Concuss();
                break;
            case BalanceState.recovering:
                ConfigureRb(4f, 4f, maxRotationForce, 0.15f);
                break;
            case BalanceState.dead:
                ConfigureRb(0.0f, 0.0f, 0.0f, 0.0f);
                Kill();
                break;
            default:
                rb.drag = 0.0f;
                rb.angularDrag = 0.0f;
                break;
        }
    }

    public void Kill()
    {
        DisableSelfCollision(false);
        ConfigureLegs(true);
        CancelInvoke();
        foreach (Transform t in transform.GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag("GrapplePoint"))
                Object.Destroy(t.gameObject);
            t.tag = "Dead";
        }
        ik.CollectGarbage();
        gameObject.AddComponent<DestroyObject>().time = 10f;
        Object.Destroy(this);
        Object.Destroy(ik);
    }

    private void ConfigureRb(float drag, float angularDrag, float rotForce, float stabilize)
    {
        if ((double)drag != -1.0)
        {
            rb.drag = drag;
            torsoRb.drag = drag;
        }
        if ((double)angularDrag != -1.0)
        {
            rb.angularDrag = angularDrag;
            torsoRb.angularDrag = angularDrag;
        }
        if ((double)rotForce != -1.0)
            rotationForce = rotForce;
        if ((double)stabilize == -1.0)
            return;
        stabilizeForce = stabilize;
    }

    public Vector3 GetVelocity()
    {
        if (rb == null)
            return Vector3.zero;
        Vector3 vel = rb.velocity * moveLegsWithSpeedScale;
        return (double)vel.magnitude > 1.0 ? vel.normalized : vel;
    }

    private void DisableSelfCollision(bool ignore)
    {
        try
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; ++i)
                for (int j = i; j < colliders.Length; ++j)
                    Physics.IgnoreCollision(colliders[i], colliders[j], ignore);
        }
        catch (System.Exception) { }
    }
}
