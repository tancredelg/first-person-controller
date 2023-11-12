using System.Collections;
using TMPro;
using UnityEngine;

public class ProjectileThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cam;
    [SerializeField] private bool useCamAsAttackPoint;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject projectile;
    [SerializeField] private TextMeshProUGUI throwsLeftText;

    [Header("Throwing")]
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    [SerializeField] private float throwForce;
    [SerializeField] private float upwardThrowForce;
    [SerializeField] private float throwCooldown;
    [SerializeField] private float reloadTime;
    [SerializeField] private int maxThrows;
    private int _throwsLeft;
    private bool _readyToThrow;
    private bool _isReloading;

    private void Awake()
    {
        _readyToThrow = true;
        if (useCamAsAttackPoint) attackPoint.position = cam.position + cam.transform.forward * transform.localScale.z * 0.6f;
        StartCoroutine(Reload());
    }

    private void Update()
    {
        if (_isReloading) return;
        if (Input.GetKeyDown(throwKey) && _readyToThrow && _throwsLeft > 0) Throw();
        if (Input.GetKeyDown(reloadKey)) StartCoroutine(Reload());
    }
    
    private void Throw()
    {
        _readyToThrow = false;
        
        Vector3 throwDirection = cam.transform.forward;

        if (!useCamAsAttackPoint && Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 500))
        {
            throwDirection = (hit.point - attackPoint.position).normalized;
        }

        Rigidbody projRb = Instantiate(projectile, attackPoint.position, cam.rotation).GetComponent<Rigidbody>();
        projRb.AddForce(throwDirection * throwForce + transform.up * upwardThrowForce, ForceMode.Impulse);

        _throwsLeft--;

        StartCoroutine(_throwsLeft == 0 ? Reload() : ResetThrow());
    }

    private IEnumerator Reload()
    {
        _isReloading = true;
        throwsLeftText.text = $"- / {maxThrows}";
        yield return new WaitForSeconds(reloadTime);
        _throwsLeft = maxThrows;
        _readyToThrow = true;
        _isReloading = false;
        throwsLeftText.text = $"{_throwsLeft} / {maxThrows}";
    }

    private IEnumerator ResetThrow()
    {
        throwsLeftText.text = $"{_throwsLeft} / {maxThrows}";
        yield return new WaitForSeconds(throwCooldown);
        _readyToThrow = true;
    }
}