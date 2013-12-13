using UnityEngine;

public class BoxyControl : MonoBehaviour
{
    public float moveForce = 365f;
    public float antiMoveForce = 200f;
    public float maxSpeed = 5f;

	private Transform groundCheck;			// A position marking where to check if the player is grounded.
	public float jumpForce = 1000f;			// Amount of force added when the player jumps.
	private bool jump = false;
	public bool grounded = false;

    void Awake()
    {
		// Setting up references.
		groundCheck = transform.Find("groundCheck");
    }

    void Update()
    {
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		grounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));  
		
		// If the jump button is pressed and the player is grounded then the player should jump.
		if(Input.GetButtonDown("Jump") && grounded)
			jump = true;
    }
	
	// If the player is changing direction (h has a different sign to velocity.x) or hasn't reached maxSpeed yet...
	//if(h * rigidbody2D.velocity.x < maxSpeed)

    void FixedUpdate()
    {
        float h = Input.GetAxis( "Horizontal" );

        rigidbody2D.AddForce( Vector2.right * h * moveForce );

        if ( Mathf.Abs( rigidbody2D.velocity.x ) > maxSpeed ) {
            rigidbody2D.velocity = new Vector2( Mathf.Sign( rigidbody2D.velocity.x ) * maxSpeed, rigidbody2D.velocity.y );
        }
		
        // do not let player slide. Apply resistance.
        if ( Mathf.Abs( h ) < Mathf.Epsilon && Mathf.Abs( rigidbody2D.velocity.x ) > maxSpeed / 3 ) {
            rigidbody2D.AddForce( Vector2.right * -Mathf.Sign( rigidbody2D.velocity.x ) * antiMoveForce );
        }

		if( jump ){
			// Add a vertical force to the player.
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
			jump = false;
		}
    }
}
