using Mirror;
using UnityEngine;
using System.Collections;

public class BorderDamage : NetworkBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
	[SerializeField] private float damageTickInterval = 1.0f;
	
	private Transform _transform;
    private Health _health;
    private Coroutine _damageCoroutine = null;
	
    [SyncVar(hook = nameof(OnOutsideStateChanged))]
    private bool _isOutside = false;
    private bool _isDamaging = false;
	
	void Awake()
    {
		_transform = GetComponent<Transform>();
        _health = GetComponent<Health>();
    }

    [ServerCallback]
    void FixedUpdate()
    {

        bool currentOutside = IsOutsideBorder();
        
        if (currentOutside != _isOutside)
        {
            _isOutside = currentOutside;
            
            if (_isOutside)
            {
                OnEnterBorderZone();
            }
            else
            {
                OnExitBorderZone();
            }
        }
    }
	
	private bool IsOutsideBorder() {
		float distance = Vector3.Distance(_transform.position, new Vector3(0.0f, 0.0f, 0.0f));
		return distance > BorderConfiguration.borderRadius;
	}
	
	[Command]
	void CmdDealDamage() {
		StopCoroutine(_damageCoroutine);
	}
	
	
	[Server]
    private IEnumerator DamageOverTimeRoutine()
    {
        float elapsedTime = 0f;
        
        while (true)
        {
            float damage = damagePerSecond * damageTickInterval;
            _health.TakeDamage(damage, DamageContext.Runaway());
            
            yield return new WaitForSeconds(damageTickInterval);
            
            elapsedTime += damageTickInterval;
        }
    }
	
	[Server]
	private void OnExitBorderZone()
    {
        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }
        
        _isDamaging = false;
    }
	
	[Server]
	private void OnEnterBorderZone()
    {
        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
        }
        
        _damageCoroutine = StartCoroutine(DamageOverTimeRoutine());
    }
	
	private void OnOutsideStateChanged(bool oldValue, bool newValue)
    {
        if (isLocalPlayer)
        {
			if (UIManager.Instance != null && !UIManager.Instance.isEndMatch) {
				if (newValue)
				{
					UIManager.Instance.ShowBorderWarning();
				}
				else
				{
					UIManager.Instance.HideBorderWarning();
				}
			}
        }
    }
	
}
