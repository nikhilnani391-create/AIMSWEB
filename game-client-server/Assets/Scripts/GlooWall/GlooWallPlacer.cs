using UnityEngine;
using FreeFire.Inventory;

namespace FreeFire.GlooWall
{
    public class GlooWallPlacer : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float maxPlacementDistance = 4f;
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private LayerMask placementMask;

        [Header("Preview")]
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerInventory playerInventory;

        private GameObject activePreview;
        private MeshRenderer previewRenderer;
        private bool isPlacementMode;
        private bool isValidPlacement;
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        public bool IsInPlacementMode => isPlacementMode;

        public void EnterPlacementMode()
        {
            if (!playerInventory.HasGlooWall()) return;

            isPlacementMode = true;

            if (previewPrefab != null && activePreview == null)
            {
                activePreview = Instantiate(previewPrefab);
                previewRenderer = activePreview.GetComponent<MeshRenderer>();
            }
        }

        public void ExitPlacementMode()
        {
            isPlacementMode = false;

            if (activePreview != null)
            {
                Destroy(activePreview);
                activePreview = null;
                previewRenderer = null;
            }
        }

        private void Update()
        {
            if (!isPlacementMode) return;

            UpdatePlacementPreview();

            if (Input.GetMouseButtonDown(0) && isValidPlacement)
            {
                ExecutePlacement();
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitPlacementMode();
            }
        }

        private void UpdatePlacementPreview()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, placementMask))
            {
                targetPosition = hit.point;

                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                isValidPlacement = slopeAngle <= maxSlopeAngle;

                Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.01f)
                {
                    forward = Vector3.forward;
                }
                targetRotation = Quaternion.LookRotation(forward, Vector3.up);

                if (activePreview != null)
                {
                    activePreview.transform.position = targetPosition;
                    activePreview.transform.rotation = targetRotation;
                    activePreview.SetActive(true);

                    if (previewRenderer != null)
                    {
                        previewRenderer.material = isValidPlacement
                            ? validPlacementMaterial
                            : invalidPlacementMaterial;
                    }
                }
            }
            else
            {
                isValidPlacement = false;
                if (activePreview != null)
                {
                    activePreview.SetActive(false);
                }
            }
        }

        private void ExecutePlacement()
        {
            if (!playerInventory.ConsumeGlooWall()) return;

            SendDeployCommand(targetPosition, targetRotation);
            ExitPlacementMode();
        }

        private void SendDeployCommand(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"[GlooWall] Deploy command: pos={position}, rot={rotation.eulerAngles}");
        }

        public (Vector3 position, Quaternion rotation) GetPlacementTarget()
        {
            return (targetPosition, targetRotation);
        }
    }
}
