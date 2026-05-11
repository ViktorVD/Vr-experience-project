using UnityEngine;

public class VRPhysicalWeapon : MonoBehaviour
{
    public enum WeaponType { Sword, Shield, Body }
    public WeaponType weaponType;
    
    [Header("Configuratie")]
    public Transform targetTransform;
    public VRCombatAgent ownerAgent;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // --- DE FIX ---
        // Alleen zwaarden en schilden moeten Kinematic zijn (niet-fysiek volgen)
        // Het lichaam (Body) moet ALTIJD fysiek blijven voor zwaartekracht en muren!
        if (rb != null)
        {
            if (weaponType == WeaponType.Sword || weaponType == WeaponType.Shield)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                
                // Zet wapen colliders op trigger
                Collider[] cols = GetComponentsInChildren<Collider>();
                foreach (var col in cols) col.isTrigger = true;
            }
            else if (weaponType == WeaponType.Body)
            {
                // Zorg dat het lichaam juist NIET kinematic is en wel gravity heeft
                rb.isKinematic = false;
                rb.useGravity = true;
                
                // Het lichaam mag GEEN trigger zijn, anders zak je door de grond!
                Collider[] cols = GetComponentsInChildren<Collider>();
                foreach (var col in cols) col.isTrigger = false;
            }
        }
    }

    public void ResetWeapon()
    {
        if (targetTransform != null && (weaponType == WeaponType.Sword || weaponType == WeaponType.Shield))
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
            Debug.Log($"[VRPhysicalWeapon] {gameObject.name} ResetWeapon gehaald naar: {targetTransform.name}");
        }
        else
        {
            Debug.Log($"[VRPhysicalWeapon] {gameObject.name} ResetWeapon is opgeroepen, maar TargetTransform is NULL of het is een Body.");
        }
    }

    void Update()
    {
        // Alleen wapens volgen hun target via transform
        if (targetTransform != null && (weaponType == WeaponType.Sword || weaponType == WeaponType.Shield))
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }
    }

    private void OnTriggerEnter(Collider otherCol)
    {
        // Zoek naar het wapen-script op het geraakte object
        VRPhysicalWeapon other = otherCol.gameObject.GetComponentInParent<VRPhysicalWeapon>();
        
        if (other == null || other.ownerAgent == this.ownerAgent) return;

        if (this.weaponType == WeaponType.Sword)
        {
            // AI moet 'aanvallen' om damage te doen. De speler mag altijd damage doen zolang hij zwaait.
            if (!ownerAgent.isAttacking && !ownerAgent.isHumanPlayer) return;

            if (other.weaponType == WeaponType.Shield)
            {
                Debug.Log("BLOCK!");
                ownerAgent.AddReward(-0.2f);
                other.ownerAgent.AddReward(0.3f);
                ownerAgent.ApplyCombatStun(0.8f); 
            }
            else if (other.weaponType == WeaponType.Body)
            {
                Debug.Log("HIT BODY!");
                ownerAgent.RegisterHitOnOpponent(25f);
                other.ownerAgent.AddReward(-0.2f);
            }
        }
    }
}
