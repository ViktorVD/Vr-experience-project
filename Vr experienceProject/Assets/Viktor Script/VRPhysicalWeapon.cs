using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VRPhysicalWeapon : MonoBehaviour
{
    public enum WeaponType { Sword, Shield, Body }
    public WeaponType weaponType;
    
    [Header("Configuratie")]
    public Transform targetTransform;
    public VRCombatAgent ownerAgent;
    
    [Header("Physics Settings")]
    public float followForce = 5000f;
    public float rotationTorque = 500f;
    public float maxVelocity = 15f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 20f;
        // Zorg dat we niet vastlopen op onszelf
        Physics.IgnoreCollision(GetComponent<Collider>(), ownerAgent.GetComponent<Collider>());
    }

    public void ResetWeapon()
    {
        if(rb == null) rb = GetComponent<Rigidbody>();
        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (ownerAgent.isStunned) 
        {
            // Tijdens stun verliest het wapen zijn actieve volging (slap effect)
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            return;
        }

        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;

        // Positie volgen via velocity (stabieler dan AddForce voor ML-Agents)
        Vector3 positionError = targetTransform.position - transform.position;
        rb.linearVelocity = positionError * followForce * Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxVelocity);

        // Rotatie volgen
        Quaternion rotationError = targetTransform.rotation * Quaternion.Inverse(transform.rotation);
        rotationError.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        
        if (angle != 0 && !float.IsNaN(axis.x))
        {
            Vector3 angularGoal = (angle * axis * Mathf.Deg2Rad) / Time.fixedDeltaTime;
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularGoal, 0.5f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        VRPhysicalWeapon otherWeapon = collision.gameObject.GetComponent<VRPhysicalWeapon>();
        
        if (otherWeapon != null && otherWeapon.ownerAgent != this.ownerAgent)
        {
            // 1. Zwaard raakt Body -> Damage!
            if (this.weaponType == WeaponType.Sword && otherWeapon.weaponType == WeaponType.Body)
            {
                ownerAgent.RegisterHitOnOpponent(10f);
                otherWeapon.ownerAgent.RegisterDamageReceived(10f);
            }
            
            // 2. Zwaard raakt Schild -> Block & Recoil
            else if (this.weaponType == WeaponType.Sword && otherWeapon.weaponType == WeaponType.Shield)
            {
                otherWeapon.ownerAgent.RegisterSuccessfulBlock();
                ownerAgent.ApplyStun(0.4f); // Aanvaller krijgt recoil
                
                // Fysieke terugslag
                rb.AddForce(-collision.contacts[0].normal * 15f, ForceMode.Impulse);
            }
            
            // 3. Zwaard raakt Zwaard -> Clash!
            else if (this.weaponType == WeaponType.Sword && otherWeapon.weaponType == WeaponType.Sword)
            {
                ownerAgent.ApplyStun(0.2f);
                otherWeapon.ownerAgent.ApplyStun(0.2f);
                
                // Fysieke terugslag voor beide
                rb.AddForce(-collision.contacts[0].normal * 8f, ForceMode.Impulse);
                otherWeapon.GetComponent<Rigidbody>().AddForce(collision.contacts[0].normal * 8f, ForceMode.Impulse);
            }
        }
    }
}
