using UnityEngine;

public enum BoxyFeeling {
	Dead,
	Normal,
	TooCool,
	Horrified
}

public class BoxyControl : MonoBehaviour
{
	public enum PlayerState {
		Walking,
		StuckOnSide,
		Jumping,
	};

	public PlayerState PreviousState = PlayerState.Walking;
	public PlayerState State = PlayerState.Walking;
	public float PlayerDecel = 0.2f; 		//The amount the player slows down
	public float PlayerAccel = 0.08f; 		// The amount the player accelerates by
    public float maxSpeed = 5f;
	public float jumpSpeed = 1f;
	public float maxJumpSpeed = 10f;

	public float[] RequiredTorque;

	public float JumpingTime = 0.0f;
	public float MaxJumpingTime = 2.0f;

	private Transform groundCheck;			// A position marking where to check if the player is grounded.
	public float jumpForce = 1000f;			// Amount of force added when the player jumps.
	public bool grounded = false;

	public Coin.CoinColor firstColor = Coin.CoinColor.None;
	private int numCoins = 0;

	public int coinsNeeded = -1;
	public string nextLevel = "";

	public BoxyFeeling feeling = BoxyFeeling.Normal;

	private Sprite normalSprite;
	private Sprite deadSprite;
	private Sprite coolSprite;
	private Sprite horrifiedSprite;

    private bool restartedSinceLastDeath = false;

    public float maxTextAnimTime = 2.0f;
    public float textAnimTimeAcc = 0.0f;

    private GUIStyle animTextStyle = new GUIStyle();

    public void Die() {
        if ( feeling == BoxyFeeling.Dead )
            return;

        feeling = BoxyFeeling.Dead;
        RagDollMe();
        restartedSinceLastDeath = false;

        if ( PlayerPrefs.GetInt( "HasDied" ) == 1 ) {
            Invoke( "RestartLevel", 1.0f );
        } else {
            // start text animation
            Invoke ( "SetDiedOnce", maxTextAnimTime );
        }
    }

    private void SetDiedOnce() {
        PlayerPrefs.SetInt( "HasDied", 1 ); 
        RestartLevel();
    }

    void OnGUI() {
        if ( feeling == BoxyFeeling.Dead && PlayerPrefs.GetInt( "HasDied" ) == 0 ) {
            GUI.Label( new Rect( Screen.width * ( 1 - textAnimTimeAcc / maxTextAnimTime ) - Screen.width, 0, Screen.width, Screen.height ), "YOU CAN ONLY CHOOSE ONE.", animTextStyle );
        }
    }

    void Awake()
    {
		// Setting up references.
		groundCheck = transform.Find("groundCheck");

        // load sprites
        normalSprite     = Resources.Load<Sprite>( "boxy" );
        deadSprite       = Resources.Load<Sprite>( "boxy_dead" );
        coolSprite       = Resources.Load<Sprite>( "boxy_deal_with_it" );
        horrifiedSprite  = Resources.Load<Sprite>( "boxy_shock" );

        // set up GUI style
        animTextStyle.fontSize = 300;
    }

	private bool IsRotated( float RotationZ )
	{
		return RotationZ <= 92  && RotationZ >= 88 
			|| RotationZ <= 182 && RotationZ >= 178
			|| RotationZ <= 272 && RotationZ >= 268;
	}

	private float GetAmountToFlip( float RotationZ )
	{
		if( RotationZ <= 92  && RotationZ >= 88  )
			return RequiredTorque[0];
		else if( RotationZ <= 182 && RotationZ >= 178 )
			return RequiredTorque[1];
		else if( RotationZ <= 272 && RotationZ >= 268 )
			return RequiredTorque[2];
		else{
			print ( "We shouldn't get here!" );
			return 0;
		}
	}

    void Update()
    {
        if ( feeling == BoxyFeeling.Dead && PlayerPrefs.GetInt( "HasDied" ) == 0 )
            textAnimTimeAcc += Time.deltaTime;
        
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		grounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));  

		//Player is stuck on his side
		if( State == PlayerState.StuckOnSide )
		{
			//Check to see if a miracle happened and the player got up.
			if ( !IsRotated( transform.rotation.eulerAngles.z ) && grounded )
			{
				PreviousState = State;
				State = PlayerState.Walking;
			}
		}
		else if( IsRotated( transform.rotation.eulerAngles.z ) && rigidbody2D.velocity.y == 0 )
		{
			PreviousState = State;
			State = PlayerState.StuckOnSide;
			rigidbody2D.velocity = new Vector2( 0, rigidbody2D.velocity.y );
		}

        // restart level if dead
        if ( Input.GetButtonDown( "Jump" ) && feeling == BoxyFeeling.Dead )
            RestartLevel();

		SpriteRenderer renderer = GetComponent<SpriteRenderer> ();

		// update sprite to reflect state
		switch( feeling ) {
		case BoxyFeeling.Dead:        renderer.sprite = deadSprite;       break;
		case BoxyFeeling.Normal:      renderer.sprite = normalSprite;     break;
		case BoxyFeeling.TooCool:     renderer.sprite = coolSprite;       break;
		case BoxyFeeling.Horrified:   renderer.sprite = horrifiedSprite;  break;
		default:                    renderer.sprite = normalSprite;     break;
		}

		if(Input.GetButtonDown("Jump"))
		{

			if( State == PlayerState.StuckOnSide )
			{
				rigidbody2D.AddTorque( GetAmountToFlip( transform.rotation.eulerAngles.z ) );
				PreviousState = State;
				State = PlayerState.Walking;
			}
			else if( grounded )
			{
				PreviousState = State;
				State = PlayerState.Jumping;
			}
		}
    }
	
	private float TendToZero(float val, float amount)
	{
		if (val > 0f && (val -= amount) < 0f) return 0f;
		if (val < 0f && (val += amount) > 0f) return 0f;
		return val;
	}

	public float SpeedX;

    void FixedUpdate()
    {
        float h = Input.GetAxis( "Horizontal" );
		SpeedX = rigidbody2D.velocity.x;
		float SpeedY = rigidbody2D.velocity.y;

		if( State != PlayerState.StuckOnSide )
		{
			if( h > 0f )
			{
				SpeedX = rigidbody2D.velocity.x + PlayerAccel + PlayerDecel;
				if( SpeedX > maxSpeed )
				{
					SpeedX = maxSpeed;
				}
			}
			else if( h < 0f )
			{
				SpeedX = rigidbody2D.velocity.x - (PlayerAccel + PlayerDecel);
				if( SpeedX < -maxSpeed )
					SpeedX = -maxSpeed;
			}

			SpeedX = TendToZero( SpeedX, PlayerDecel );
		}
		if( State == PlayerState.Jumping ){
			if( PreviousState != PlayerState.Jumping )
			{
				rigidbody2D.AddForce(new Vector2(0f, jumpForce));
				PreviousState = State;
				return;
			}
			else if( Input.GetButton("Jump") )
			{
				SpeedY += jumpSpeed;
				JumpingTime += Time.fixedDeltaTime;
				if( JumpingTime >= MaxJumpingTime )
				{
					State = PlayerState.Walking;
					JumpingTime = 0f;
				}
			}
			else //Not jumping anymore, so begin descent
			{
				State = PlayerState.Walking;
				JumpingTime = 0f;
			}
		}

		if( rigidbody2D.velocity.x != SpeedX || rigidbody2D.velocity.y != SpeedY )
		{
			rigidbody2D.velocity = new Vector2( SpeedX, SpeedY );
		}
    }


    private void LoadNextLevel() {
        Application.LoadLevel( nextLevel );
    }

    private void RestartLevel() {
        if ( !restartedSinceLastDeath ) {
            Application.LoadLevel(Application.loadedLevel);
            restartedSinceLastDeath = true;
        }
    }

    // 2D ragdoll FTW
    private void RagDollMe() {
        if ( !grounded ) {
            // choose random torque direction, then choose a random torque value between 25 and 200
            rigidbody2D.AddTorque( Mathf.Sign( Random.value - 0.5f ) * ( 25f + Random.value * 175f ) );
        } else {
            // fall down in the direction we're going
            rigidbody2D.AddTorque( Mathf.Sign( rigidbody2D.velocity.x ) * -50f );
        }
    }

    private string colorToTag( Coin.CoinColor color ) {
        switch (color)
        {
            case Coin.CoinColor.Blue:
                    return "Blue Coin";
            case Coin.CoinColor.Green:
                    return "Green Coin";
            case Coin.CoinColor.Orange:
                    return "Orange Coin";
            case Coin.CoinColor.Red:
                    return "Red Coin";
            case Coin.CoinColor.None:
                    return "White Coin";
            default: return "White Coin";
        }
    }

    private int getCoinsInLevel( Coin.CoinColor color ) {
        return GameObject.FindGameObjectsWithTag( colorToTag( color ) ).Length;
    }

	public bool HandleGetCoin( Coin.CoinColor color ) {

        if ( feeling == BoxyFeeling.TooCool || feeling == BoxyFeeling.Dead ) {
            return false; //don't get no mo coins
        }

		if (firstColor == Coin.CoinColor.None) {
			firstColor = color;
            coinsNeeded = getCoinsInLevel( color );
		}
			
		if (firstColor == color) {
			numCoins++;
			if ( numCoins == coinsNeeded ) {
                feeling = BoxyFeeling.TooCool;
                Invoke( "LoadNextLevel", 1.0f );
			}
		} else {
            Die();
		}

        return true;
	}
}
