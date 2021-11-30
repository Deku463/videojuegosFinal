using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public ParticleSystem _dustParticles;
    public ParticleSystem _dashParticles;
    public ParticleSystem _wallParticles;
    public PlayerUI _ui;

    [Header("Components")]
    private Rigidbody2D _rb;
    private Animator _anim;
    private BoxCollider2D _col;

    [Header("Layer masks")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _cornerCorrectLayer;
    [SerializeField] private LayerMask _wallLayer;

    [Header("Movement variables")]
    [SerializeField] private float _movementAcceleration = 50;
    [SerializeField] private float _maxMoveSpeed = 12;
    [SerializeField] private float _groundLinearDrag = 10;
    private float _horizontalDirection;
    private float _verticalDirection;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);
    private bool _facingRight = true;

    [Header("Jump variables")]
    [SerializeField] private float _jumpforce = 12f;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 8f;
    [SerializeField] private float _lowJumpFallMultiplier = 5f;
    [SerializeField] private int _extraJumps = 1;
    [SerializeField] private float _hangTime = 0.1f;
    [SerializeField] private float _jumpBufferLength = 0.1f;
    private int _extraJumpsValue;
    private float _hangTimeCounter;
    private float _jumpBufferCounter;
    private bool _canJump => _jumpBufferCounter > 0f && (_hangTimeCounter > 0f || _extraJumpsValue > 0 || _onWall);
    private bool _isJumping = false;

    [Header("Wall movement variables")]
    [SerializeField] private float _wallSlideModifier = 0.5f;
    [SerializeField] private float _wallJumpXVelocityHaltDelay = 0.2f;
    private bool _wallSlide => _onWall && !_onGround && _rb.velocity.y < 0f;

    [Header("Dash Variables")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashLength = .3f;
    [SerializeField] private float _dashBufferLength = .1f;
    private float _dashBufferCounter;
    private bool _isDashing;
    private bool _hasDashed;
    private bool _canDash => _dashBufferCounter > 0f && !_hasDashed;

    [Header("Shooting variables")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _shootBufferLength = .5f;
    [SerializeField] private Transform _standingFirePoint;
    [SerializeField] private Transform _runningFirePoint;
    [SerializeField] private Transform _slidingFirePoint;
    private Transform _firePoint;
    private float _shootBufferCounter;
    private bool _isShooting;
    private bool _canShoot => _shootBufferCounter > 0f && (_onGround || _onWall);

    [Header("Ground collision variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private Vector3 _groundRaycastOffset;
    private bool _onGround;

    [Header("Wall collision variables")]
    [SerializeField] private float _wallRaycastLength;
    public bool _onWall;
    public bool _onRightWall;

    [Header("Corner correction variables")]
    [SerializeField] private float _topRaycastLength;
    [SerializeField] private Vector3 _edgeRaycastOffset;
    [SerializeField] private Vector3 _innerRaycastOffset;
    private bool _canCornerCorrect;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _col = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        _horizontalDirection = GetInput().x;
        _verticalDirection = GetInput().y;
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = _jumpBufferLength;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Dash")) 
        {
            _dashBufferCounter = _dashBufferLength;
        }
        else
        {
            _dashBufferCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            _shootBufferCounter = _shootBufferLength;
        }
        else
        {
            _shootBufferCounter -= Time.deltaTime;
        }
        Animation();
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        if (_canDash) 
            StartCoroutine(Dash(_horizontalDirection, _verticalDirection));

        if (_canShoot)
            StartCoroutine(Shoot());
            _shootBufferCounter = 0f;

        if (!_isDashing)
        {
            MoveCharacter();
            if (_onGround)
            {
                _extraJumpsValue = _extraJumps;
                ApplyGroundLinearDrag();
                _hangTimeCounter = _hangTime;
                _hasDashed = false;
            }
            else
            {
                ApplyAirLinearDrag();
                FallMultiplier();
                _hangTimeCounter -= Time.fixedDeltaTime;
                if (!_onWall || _rb.velocity.y < 0f) _isJumping = false;
            }

            if (_canJump)
            {
                if (_onWall && !_onGround)
                {
                    if ((_onRightWall && _horizontalDirection > 0f || !_onRightWall && _horizontalDirection < 0f))
                    {
                        StartCoroutine(NeutralWallJump());
                    }
                    else
                    {
                        WallJump();
                    }
                    Flip();
                }
                else
                {
                    Jump(Vector2.up);
                }
            }
            if (!_isJumping)
            {
                if (_wallSlide) WallSlide();
                if (_onWall) StickToWall();
            }
        }
        if (_canCornerCorrect) CornerCorrect(_rb.velocity.y);
    }

    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void MoveCharacter()
    {
        _rb.AddForce(new Vector2(_horizontalDirection, 0f) * _movementAcceleration);
        
        if(Mathf.Abs(_rb.velocity.x) > _maxMoveSpeed)
        {
            _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * _maxMoveSpeed, _rb.velocity.y);
        }
    }

    private void ApplyGroundLinearDrag()
    {
        if(Mathf.Abs(_horizontalDirection) < 0.4f || _changingDirection)
        {
            _rb.drag = _groundLinearDrag;
        }
        else
        {
            _rb.drag = 0f;
        }
    }

    private void ApplyAirLinearDrag()
    {
        _rb.drag = _airLinearDrag;
    }

    private void Jump(Vector2 direction)
    {
        if (!_onGround && !_onWall)
        {
            _extraJumpsValue--;
        }

        ApplyAirLinearDrag();
        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        _rb.AddForce(direction * _jumpforce, ForceMode2D.Impulse);
        _hangTimeCounter = 0f;
        _jumpBufferCounter = 0f;
        _isJumping = true;
    }
    private void WallJump()
    {
        Vector2 jumpDirection = _onRightWall ? Vector2.left : Vector2.right;
        Jump(Vector2.up + jumpDirection);
    }
    IEnumerator NeutralWallJump()
    {
        Vector2 jumpDirection = _onRightWall ? Vector2.left : Vector2.right;
        Jump(Vector2.up + jumpDirection);
        yield return new WaitForSeconds(_wallJumpXVelocityHaltDelay);
        _rb.velocity = new Vector2(0f, _rb.velocity.y);
    }

    private void FallMultiplier()
    {
        if (_rb.velocity.y < 0f)
        {
            _rb.gravityScale = _fallMultiplier;
        }
        else if(_rb.velocity.y > 0f && !Input.GetButton("Jump"))
        {
            _rb.gravityScale = _lowJumpFallMultiplier;
        }
        else
        {
            _rb.gravityScale = 1f;
        }
    }

    void WallSlide()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, -_maxMoveSpeed * _wallSlideModifier);
    }

    void StickToWall()
    {
        //Push player torwards wall
        if (_onRightWall && _horizontalDirection >= 0f)
        {
            _rb.velocity = new Vector2(1f, _rb.velocity.y);
        }
        else if (!_onRightWall && _horizontalDirection <= 0f)
        {
            _rb.velocity = new Vector2(-1f, _rb.velocity.y);
        }

        //Face correct direction
        if (_onRightWall && !_facingRight)
        {
            Flip();
        }
        else if (!_onRightWall && _facingRight)
        {
            Flip();
        }

        _wallParticles.Play();
    }

    void Flip()
    {
        _facingRight = !_facingRight;
        transform.Rotate(0f, 180f, 0f);
        //if(_onGround) _dustParticles.Play();
    }

    IEnumerator Dash(float x, float y)
    {
        float dashStartTime = Time.time;
        _hasDashed = true;
        _isDashing = true;
        _isJumping = false;

        _dashParticles.Play();
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = 0f;
        _rb.drag = 0f;

        Vector2 dir;
        if (x != 0f || y != 0f)
        {
            dir = new Vector2(x, y);
        }
        else
        {
            if (_facingRight) dir = new Vector2(1f, 0f);
            else dir = new Vector2(-1f, 0f);
        }

        while(Time.time < dashStartTime + _dashLength)
        {
            _rb.velocity = dir.normalized * _dashSpeed;
            yield return null;
        }

        _isDashing = false;
    }

    IEnumerator Shoot()
    {
        Transform _shotPosition;


        if (_wallSlide)
        {
            _shotPosition = _slidingFirePoint;
        }
        else if (Mathf.Abs(_horizontalDirection) > 0)
        {
            _shotPosition = _runningFirePoint;
        }
        else
        {
            _shotPosition = _standingFirePoint;
        }

        _isShooting = true;
        Instantiate(_bulletPrefab, _shotPosition.position, _shotPosition.rotation);
        yield return new WaitForSeconds(0.5f);
        _isShooting = false;
    }

    void Animation()
    {
        if (_isDashing)
        {
            _anim.SetBool("isDashing", true);
            _anim.SetBool("isGrounded", false);
            _anim.SetBool("WallGrab", false);
            _anim.SetBool("isJumping", false);
            _anim.SetFloat("horizontalDirection", 0f);
            _anim.SetFloat("verticalDirection", 0f);
            _anim.SetBool("isShooting", false);
        }
        else
        {
            _anim.SetBool("isDashing", false);

            if ((_horizontalDirection < 0f && _facingRight || _horizontalDirection > 0f && !_facingRight) && !_wallSlide)
            {
                Flip();
            }
            if (_onGround)
            {
                _anim.SetBool("isGrounded", true);
                _anim.SetBool("WallGrab", false);
                _anim.SetFloat("horizontalDirection", Mathf.Abs(_horizontalDirection));
            }
            else
            {
                _anim.SetBool("isGrounded", false);
            }
            if (_isJumping)
            {
                _anim.SetBool("isJumping", true);
                _anim.SetBool("WallGrab", false);
                _anim.SetFloat("verticalDirection", 0f);
                _anim.SetBool("isShooting", false);
            }
            else
            {
                _anim.SetBool("isJumping", false);

                if (_wallSlide)
                {
                    _anim.SetBool("WallGrab", true);
                    _anim.SetFloat("verticalDirection", 0f);
                }
                else if (_rb.velocity.y < 0f)
                {
                    _anim.SetBool("WallGrab", false);
                    _anim.SetFloat("verticalDirection", 0f);
                }
                if (_isShooting)
                {
                    _anim.SetBool("isShooting", true);
                }
                else
                {
                    _anim.SetBool("isShooting", false);
                }
            }
        }
    }

    void CornerCorrect(float Yvelocity)
    {
        RaycastHit2D _hit = Physics2D.Raycast(transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength, Vector3.left, _topRaycastLength, _cornerCorrectLayer);
        if (_hit.collider != null)
        {
            float _newPos = Vector3.Distance(new Vector3(_hit.point.x, transform.position.y, 0f) + Vector3.up * _topRaycastLength,
                transform.position - _edgeRaycastOffset + Vector3.up * _topRaycastLength);
            transform.position = new Vector3(transform.position.x + _newPos, transform.position.y, transform.position.z);
            _rb.velocity = new Vector2(_rb.velocity.x, Yvelocity);
            return;
        }

        _hit = Physics2D.Raycast(transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength, Vector3.right, _topRaycastLength, _cornerCorrectLayer);
        if (_hit.collider != null)
        {
            float _newPos = Vector3.Distance(new Vector3(_hit.point.x, transform.position.y, 0f) + Vector3.up * _topRaycastLength,
                transform.position + _edgeRaycastOffset + Vector3.up * _topRaycastLength);
            transform.position = new Vector3(transform.position.x - _newPos, transform.position.y, transform.position.z);
            _rb.velocity = new Vector2(_rb.velocity.x, Yvelocity);
        }
    }

    private void CheckCollisions()
    {
        //Ground collisions
        _onGround = Physics2D.Raycast(transform.position + _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer) ||
                    Physics2D.Raycast(transform.position - _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer);

        //Corner collisions
        _canCornerCorrect = Physics2D.Raycast(transform.position + _edgeRaycastOffset, Vector2.up, _topRaycastLength, _cornerCorrectLayer) &&
                            !Physics2D.Raycast(transform.position + _innerRaycastOffset, Vector2.up, _topRaycastLength, _cornerCorrectLayer) ||
                            Physics2D.Raycast(transform.position - _edgeRaycastOffset, Vector2.up, _topRaycastLength, _cornerCorrectLayer) &&
                            !Physics2D.Raycast(transform.position - _innerRaycastOffset, Vector2.up, _topRaycastLength, _cornerCorrectLayer);

        //Wall collisions
        _onWall = Physics2D.Raycast(transform.position, Vector2.right, _wallRaycastLength, _wallLayer) ||
                    Physics2D.Raycast(transform.position, Vector2.left, _wallRaycastLength, _wallLayer);
        _onRightWall = Physics2D.Raycast(transform.position, Vector2.right, _wallRaycastLength, _wallLayer);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        //Ground check
        Gizmos.DrawLine(transform.position + _groundRaycastOffset, transform.position + _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        Gizmos.DrawLine(transform.position - _groundRaycastOffset, transform.position - _groundRaycastOffset + Vector3.down * _groundRaycastLength);

        //Corner Check
        Gizmos.DrawLine(transform.position + _edgeRaycastOffset, transform.position + _edgeRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position - _edgeRaycastOffset, transform.position - _edgeRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position + _innerRaycastOffset, transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position - _innerRaycastOffset, transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength);

        //Corner Distance Check
        Gizmos.DrawLine(transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength,
                        transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength + Vector3.left * _topRaycastLength);
        Gizmos.DrawLine(transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength,
                        transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength + Vector3.right * _topRaycastLength);

        //Wall Check
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * _wallRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * _wallRaycastLength);

        //Fire point
        if (_wallSlide)
        {
            Gizmos.DrawCube(_slidingFirePoint.position, Vector3.one * 0.2f);
        }
        else if (Mathf.Abs(_horizontalDirection) > 0)
        {
            Gizmos.DrawCube(_runningFirePoint.position, Vector3.one * 0.2f);
        }
        else
        {
            Gizmos.DrawCube(_standingFirePoint.position, Vector3.one * 0.2f);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            _ui.TakeDamage(10);
        }
    }

}

