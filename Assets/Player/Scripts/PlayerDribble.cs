using UnityEngine;

public class PlayerDribble : MonoBehaviour
{
    public BallDribble ball;
    [SerializeField] KeyCode keyToDribble;
    public Animator animator;
    private bool dribbling;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        dribbling = false;
    }

    public void CatchBall()
    {
        if (ball != null && (animator.GetBool("IsJumping") == false) && ball != null && (animator.GetBool("IsSliding") == false))
        {
            ball.TryCatchBall();
        }
    }

    public void DropBall()
    {
        if (ball != null && (animator.GetBool("IsJumping") == false) && ball != null && (animator.GetBool("IsSliding") == false))
        {
            ball.BounceBall();
        }
    }   

    void FixedUpdate () {

        if (Input.GetKey(keyToDribble))
        {
            dribbling = true;
            animator.SetBool("IsDribbling", true);
        }
        else 
        {
            animator.SetBool("IsDribbling", false);
            ball.ReleaseBall();
        }
    }

}
