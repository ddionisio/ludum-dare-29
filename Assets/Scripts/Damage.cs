using UnityEngine;
using System.Collections;

public class Damage : MonoBehaviour {
    public const string DamageMessage = "OnDamage";

    public float amount;

    public bool CallDamageTo(Stats stat, Vector3 hitPos, Vector3 hitNorm) {
        return stat.ApplyDamage(this, hitPos, hitNorm);
    }

    public bool CallDamageTo(GameObject target, Vector3 hitPos, Vector3 hitNorm) {
        //target.SendMessage(DamageMessage, this, SendMessageOptions.DontRequireReceiver);
        Stats stat = target.GetComponent<Stats>();
        if(stat) {
            return CallDamageTo(stat, hitPos, hitNorm);
        }

        return false;
    }
}
