using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharController : MonoBehaviour
{
    private CharacterController characterController;

    public float speed;
    public float lookSmooth;
    public float directionSmooth;

    private Vector3 inputDirection = Vector3.zero;
    private Vector3 effectiveDirection = Vector3.zero;
    private float gravityValue = -25f;
    private float jumpHeight = 5.0f;

    public Camera cam;

    bool charIsGrounded;
    public bool grinding;
    public bool grindingReverse;
    public bool canGrind;

    public List<Vector3> grindingNodes;
    public int nodeNum;
    public Vector3 nodeOffset;

    public float grindSpeed;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (characterController) {
            if (grinding) {
                grindingMovement();
                if (Input.GetButtonDown("Jump")) {
                    grinding = false;
                StartCoroutine(grindCooldown());
                }
            }
            else if (!grinding) {
                handleMovement();
            }
            charIsGrounded = characterController.isGrounded;
        }
    }

    void handleMovement() {
        float movementHorizontal = Input.GetAxisRaw("Horizontal");
        float movementVertical = Input.GetAxisRaw("Vertical");
        
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward = forward.normalized;
        right = right.normalized;

        Vector3 cameraRelativeMovement = right * movementHorizontal + forward * movementVertical;
        
        transform.Translate(cameraRelativeMovement * 10f * Time.deltaTime);

        if (cameraRelativeMovement.magnitude > 0.01)
        {
            float lookAngle = Mathf.Atan2(cameraRelativeMovement.x, cameraRelativeMovement.z) * Mathf.Rad2Deg;
            float effectiveAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, lookAngle, lookSmooth);
            transform.rotation = Quaternion.Euler(0, effectiveAngle, 0);
        }

        effectiveDirection = Vector3.Lerp(effectiveDirection, cameraRelativeMovement, directionSmooth);

        if (characterController.isGrounded && effectiveDirection.y < 0)
        {
            effectiveDirection.y = 0f;
        }

        if (Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            effectiveDirection.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }
        effectiveDirection.y += gravityValue * Time.deltaTime;

        characterController.Move(new Vector3(effectiveDirection.x * speed * Time.deltaTime, effectiveDirection.y * Time.deltaTime, effectiveDirection.z * speed * Time.deltaTime));
    }

    void grindingMovement() {
        if (!grindingReverse) {
            if (nodeNum < grindingNodes.Count - 2 && Vector3.Distance(transform.position, grindingNodes[nodeNum + 1] + nodeOffset) < 0.1f) {
                nodeNum++;
            }
            else if (nodeNum == grindingNodes.Count - 2 && Vector3.Distance(transform.position, grindingNodes[nodeNum + 1] + nodeOffset) < 0.1f) {
                grinding = false;
                var heading = grindingNodes[nodeNum + 1] - grindingNodes[nodeNum];
                heading = heading.normalized;
                heading += new Vector3(0, 0.5f, 0);
                effectiveDirection += heading;
                StartCoroutine(grindCooldown());
            }
            transform.position = Vector3.MoveTowards(transform.position, grindingNodes[nodeNum + 1] + nodeOffset, grindSpeed * Time.deltaTime);
            var lookPos = grindingNodes[nodeNum + 1] + nodeOffset - transform.position;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }
        else {
            if (nodeNum > 1 && Vector3.Distance(transform.position, grindingNodes[nodeNum - 1] + nodeOffset) < 0.1f) {
                nodeNum--;
            }
            else if (nodeNum == 1 && Vector3.Distance(transform.position, grindingNodes[nodeNum - 1] + nodeOffset) < 0.1f) {
                grinding = false;
                var heading = grindingNodes[nodeNum - 1] - grindingNodes[nodeNum];
                heading = heading.normalized;
                heading += new Vector3(0, 0.5f, 0);
                effectiveDirection += heading;
                StartCoroutine(grindCooldown());
            }
            transform.position = Vector3.MoveTowards(transform.position, grindingNodes[nodeNum - 1] + nodeOffset, grindSpeed * Time.deltaTime);
            var lookPos = grindingNodes[nodeNum - 1] + nodeOffset - transform.position;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }
    }

    void OnTriggerEnter(Collider hit) {
        if (hit.gameObject.tag == "Rail" && !grinding && canGrind) {

            Debug.Log("Hit rail");

            List<Vector3> nodes = hit.gameObject.GetComponent<Rail>().nodes;

            float distance = Vector3.Distance(transform.position, nodes[0] + hit.transform.position);

            Vector3 closestNode = nodes[0] + hit.transform.position;

            nodeOffset = hit.transform.position - new Vector3(0, 0.5f, 0);
            nodeNum = 0;

            foreach (Vector3 node in nodes) {
                if (Vector3.Distance(transform.position, node+ hit.transform.position) < distance) {
                    distance = Vector3.Distance(transform.position, node+ hit.transform.position);
                    closestNode = node + hit.transform.position;
                    nodeNum = nodes.IndexOf(node);
                }
            }

            if (nodeNum == 0) {
                grindingReverse = false;
            }
            else if (nodeNum == nodes.Count - 1) {
                grindingReverse = true;
            }
            else {
                var dirToNextNode = Vector3.Normalize(nodes[nodeNum+1] - transform.position);
                var dirToPrevNode = Vector3.Normalize(nodes[nodeNum-1] - transform.position);
                var dotToNext = Vector3.Dot(transform.forward, dirToNextNode);
                var dotToPrev = Vector3.Dot(transform.forward, dirToPrevNode);
                if (dotToNext >= dotToPrev){
                    grindingReverse = false;
                }
                else {
                    grindingReverse = true;
                }
            }

            transform.position = closestNode + new Vector3(0, nodeOffset.y - 0.5f, 0);

            grinding = true;
            grindingNodes = nodes;

            Debug.Log(closestNode);
        }
    }

    IEnumerator grindCooldown() {
        canGrind = false;
        yield return new WaitForSeconds(0.2f);
        canGrind = true;
    }
}
