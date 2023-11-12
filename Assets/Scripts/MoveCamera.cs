using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    private void Update() => transform.position = camTransform.position;
}