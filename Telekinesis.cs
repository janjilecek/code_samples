using UnityEngine;
using Random = UnityEngine.Random;

public class Telekinesis : MonoBehaviour
{
    public Camera mainCamera;

    [Header("Interaction controls")] 
    public float interactionDistance;
    public Transform holdPosition;
    public float attractionSpeed;
    public float minThrowForce;
    public float maxThrowForce;
    public AudioClip[] sounds;

    [Header("Functional vars")] 
    public GameObject heldObject;
    public BoxSpawner boxSpawner;
    public bool holdsObject = false;

    [Header("Cached variables")] private float _throwForce;
    private float len = 20f; // global interaction distance
    private AudioSource _source;
    private Rigidbody _rbOfHeldObject;
    private Vector3 _rotateVector = Vector3.one;
    private LineRenderer _lineRenderer;
    private int _thrownBoxes = 3; // controls throwns boxes and their spawn

    void Start()
    {
        _throwForce = minThrowForce;
        _lineRenderer = new LineRenderer();
        _source = GetComponent<AudioSource>();
    }


    void Update()
    {
        // simple controls will suffice
        if (Input.GetMouseButtonDown(0))
        {
            drawLine();
        }

        if (Input.GetMouseButtonDown(0) && !holdsObject)
        {
            Raycast();
        }

        if (Input.GetMouseButton(1) && holdsObject)
        {
            float lightSpeed = 35f;
            float amount = 0.001f;
            float randSin;

            randSin = Mathf.Sin(Time.time * lightSpeed) * amount;
            heldObject.transform.position = new Vector3(heldObject.transform.position.x,
                heldObject.transform.position.y + randSin, heldObject.transform.position.z);


            float diff = 0.001f;
            _throwForce += 0.1f;
            _rotateVector = new Vector3(_rotateVector.x + diff, _rotateVector.y + diff, _rotateVector.z + diff);
        }

        if (Input.GetMouseButtonUp(1) && holdsObject)
        {
            ShootObject();
        }

        if (Input.GetKeyDown(KeyCode.F) && holdsObject)
        {
            ReleaseObject();
        }

        if (holdsObject)
        {
            RotateObject();

            if (CheckDistance() >= 0.01f)
            {
                MoveObjectToPosition();
            }
        }
    }


    // ----------------------------- POLISHING SECTION 
    private void CalculateRotationVector()
    {
        float x = Random.Range(-0.5f, 0.5f); // will rotate with different speed
        float y = Random.Range(-0.5f, 0.5f);
        float z = Random.Range(-0.5f, 0.5f);

        _rotateVector = new Vector3(x, y, z);
    }

    private void RotateObject()
    {
        heldObject.transform.Rotate(_rotateVector);
    }


    // ---------------------------------- FUNCTIONAL SECTION
    public float CheckDistance()
    {
        return Vector3.Distance(heldObject.transform.position, holdPosition.transform.position);
    }

    private void MoveObjectToPosition()
    {
        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, holdPosition.transform.position,
            attractionSpeed * Time.deltaTime);
    }

    public void ReleaseObject()
    {
        _source.Stop();
        _rbOfHeldObject.constraints = RigidbodyConstraints.None;
        heldObject.transform.parent = null;
        heldObject = null;
        holdsObject = false;
    }

    private void ShootObject()
    {
        _throwForce = Mathf.Clamp(_throwForce, minThrowForce, maxThrowForce);
        Vector3 dis = Input.mousePosition - holdPosition.transform.position;

        //Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //Vector3 holdPos = mainCamera.ScreenToWorldPoint(holdPosition.transform.position);

        Vector3 holdPos = mainCamera.WorldToViewportPoint(holdPosition.transform.position);
        Vector3 mousePos = mainCamera.WorldToViewportPoint(Input.mousePosition);

        Vector3 res = mousePos - holdPos;
        //Vector3 throwvector = GetProperMousePos() - holdPosition.position;
        //throwvector.z = 1f;

        Vector3 pos = drawLine();
        if (pos != Vector3.zero)
        {
            _thrownBoxes--;
            if (_thrownBoxes == 0)
            {
                boxSpawner.spawnBoxes();
                _thrownBoxes = 3;
            }

            Vector3 throwvector = pos - holdPosition.position;
            Debug.Log("throwvector " + throwvector);
            _rbOfHeldObject.AddForce(pos * _throwForce, ForceMode.Impulse);
            _throwForce = minThrowForce;
            Destroy(heldObject, 3); // destroy after 3 seconds
        }

        // TODO add failed sound effect
        ReleaseObject();

        _source.PlayOneShot(sounds[2]);
        Camera.main.GetComponent<CameraShake>().shakeAmount = Random.Range(0.2f, 0.35f);
        Camera.main.GetComponent<CameraShake>().shakeDuration = 0.08f;
    }


    Vector3 drawLine()
    {
        Vector3 worldMousePosition =
            mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, len));
        Vector3 direction = worldMousePosition - holdPosition.transform.position;
        Debug.DrawLine(holdPosition.transform.position, worldMousePosition, Color.green, 0.5f);
        return direction;
    }

    Vector3 GetProperMousePos()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 point = ray.origin + (ray.direction * 10);
        return point;
    }

    private void Raycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        //Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, ~LayerMask.NameToLayer("enviro")))
        {
            if (hit.collider.CompareTag("Box"))
            {
                heldObject = hit.collider.gameObject;
                heldObject.transform.SetParent(holdPosition);

                _rbOfHeldObject = heldObject.GetComponent<Rigidbody>();
                _rbOfHeldObject.constraints = RigidbodyConstraints.FreezeAll; // we want it to be stuck
                holdsObject = true;

                _source.PlayOneShot(sounds[0]);
                _source.Play();

                CalculateRotationVector();
            }
        }
    }
}