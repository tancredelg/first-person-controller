using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float maxTimeToHit;
    [SerializeField] private float timeToDespawnAfterHit;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private bool _targetWasHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        Destroy(gameObject, maxTimeToHit + timeToDespawnAfterHit);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_targetWasHit || playerLayer.value == 1 << collision.gameObject.layer) return;
        _targetWasHit = true;
        _rigidbody.isKinematic = true;
        _collider.isTrigger = true;
        transform.SetParent(collision.transform);
        StartCoroutine(Despawn());
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSeconds(timeToDespawnAfterHit);
        int frames = Mathf.RoundToInt(0.5f / Time.smoothDeltaTime);
        Vector3 originalScale = transform.localScale;
        Debug.Log(frames);
        for (int i = 0; i < frames; i++)
        {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, (float)i / frames);
            yield return null;
        }
        Destroy(gameObject);
    }
}