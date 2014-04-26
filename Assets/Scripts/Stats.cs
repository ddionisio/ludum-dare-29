using UnityEngine;
using System.Collections;

public class Stats : MonoBehaviour {
    public delegate void ChangeCallback(Stats stat, float delta);
    public delegate void ApplyDamageCallback(Damage damage);

    public float maxHP;

    public event ChangeCallback changeHPCallback;
    public event ApplyDamageCallback applyDamageCallback;

    protected Damage mLastDamage;
    protected Vector3 mLastDamagePos;
    protected Vector3 mLastDamageNorm;

    protected float mCurHP;
    private bool mIsInvul;

    public float curHP {
        get { return mCurHP; }

        set {
            float v = Mathf.Clamp(value, 0, maxHP);
            if(mCurHP != v) {
                float prev = mCurHP;
                mCurHP = v;

                if(changeHPCallback != null)
                    changeHPCallback(this, mCurHP - prev);
            }
        }
    }

    public virtual bool isInvul { get { return mIsInvul; } set { mIsInvul = value; } }

    public Damage lastDamageSource { get { return mLastDamage; } }

    /// <summary>
    /// This is the latest damage hit position when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamagePosition { get { return mLastDamagePos; } }

    /// <summary>
    /// This is the latest damage hit normal when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamageNormal { get { return mLastDamageNorm; } }

    protected void ApplyDamageEvent(Damage damage) {
        if(applyDamageCallback != null)
            applyDamageCallback(damage);
    }

    public virtual bool ApplyDamage(Damage damage, Vector3 hitPos, Vector3 hitNorm) {
        mLastDamage = damage;
        mLastDamagePos = hitPos;
        mLastDamageNorm = hitNorm;

        if(!isInvul && mCurHP > 0.0f) {
            float amt = damage.amount;

            if(amt > 0.0f) {
                curHP -= amt;

                ApplyDamageEvent(damage);

                return true;
            }
            else {
                /*if(!string.IsNullOrEmpty(damage.noDamageSfx))
                    SoundPlayerGlobal.instance.Play(damage.noDamageSfx);

                if(!string.IsNullOrEmpty(damage.noDamageSpawnGroup) && !string.IsNullOrEmpty(damage.noDamageSpawnType)) {
                    PoolController.Spawn(damage.noDamageSpawnGroup, damage.noDamageSpawnType, null, null, new Vector2(hitPos.x, hitPos.y));
                }*/
            }
        }

        return false;
    }

    public virtual void Reset() {
        curHP = maxHP;
        mIsInvul = false;
        mLastDamage = null;
    }

    protected virtual void OnDestroy() {
        changeHPCallback = null;
        applyDamageCallback = null;
    }

    protected virtual void Awake() {
        mCurHP = maxHP;
    }
}
