using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CivilianInput : MonoBehaviour {
	
	//AI controller for enemies. Blender build's AI seems to be a state machine
	//could do some crazy interface where each state has a BehaviorUpdate(), but that's probably not worth the work
	public GameObject Actor = null;
	public GameObject Target = null;
	public List<GameObject> Drops = null;
	public float GunAlignmentDistance = 1000;
	public float MoveForce = 3;
	public float TrackingSpeed = 1.0f;
	public float Speed = 1.0f;
	
	private ActorController actorCtrl;
	private AnimationProcessor processor;
	private HealthInfo healthInfo;
	private enum EnemyState { Normal, Track, Strafe, Backpedal, InterceptMissile };
	private GameObject interceptTarget = null; //temporary target assigned when something must be intercepted (i.e., missiles)
	private EnemyState enemyState = EnemyState.Normal;
	private float stateChangeTimer = 0; //measured in seconds
	private float missileLaunchTimer = 0;
	
	private const float stateChangeTimeLimit = 5.0f;
	public float missileFireRate = 1.0f;
	
	private float sqrGoToTrackDist = Mathf.Pow(10.0f, 2);
	private float sqrGoToStrafeDist = Mathf.Pow(59.0f, 2);
	private float sqrGoToNormalDist = Mathf.Pow(15.0f, 2);
	public float AttackRange = 50.0f;
	private int agentID;
	
	// Added by Mike D. 12:44 am sat
	public enum State {Walk, Idle, Panic, Dead, HandsUp};
	public PlayerInput masterPlayer;
	private float DeathTimer = -100;
	
	//missile tracking/statistics members
	Dictionary<int, GameObject> missiles;
	//negative value indicates no missiles locked onto agent
	float sqrAvgMissileDist = -1.0f;
	int numMissilesToIntercept = 0;
	
	// ADDED BY MIKE D
	public void SetMasterPlayer(PlayerInput P){
		masterPlayer = P;
	}
	
	/*
	 * Mike D: This probably isn't how signals will work,
	 * but this is how the bots should react.
	 * Also, I don't know where this goes, maybe in the
	 * update section?
	 * if (IsHandsUpState())
	 * {
	 * 		if (GlobalCorpses > 0)
	 * 			State = State.Panic;
	 * 		else
	 * 			State = State.Normal;
	 * }
	 * 
	 * if (DeathTimer >= 0)
	 * {
	 * 		DeathTimer -= somevalue
	 * }
	 * else if (DeathTimer > -99)
	 * {
	 * 		GLOBAL BODYCOUNT --;
	 * 		Delete or become invisible
	 * }
	 * 
	 */
	
	public void Jump()
	{
		;;
	}
	
	public void Kill()
	{
		GLOBAL_BODYCOUNT ++;
		//PlayDeathAnimation()
		DeathTimer = 10; // or something
	}
	
	
	//properties for missile tracking/statistics
	public float SqrAvgMissileDist
	{
		get { return sqrAvgMissileDist; }
	}
	public int NumMissilesToIntercept
	{
		get { return numMissilesToIntercept; }
	}
	public float MissileDensity
	{
		get
		{
			float msslVolume = OrdinanceManager.GetMissileVolumeTrackingTarget(Actor);
			if(msslVolume > 0.0f)
			{
				return numMissilesToIntercept / msslVolume;
			}
			return 0.0f;
		}
	}
	/*
	public static void AddEnemy(Vector3 position, GameObject target)
	{
		
		EnemyController e = Instantiate(EnemyController, position);
	}*/
	
	// Use this for initialization
	void Start () {
		//add us to the global AI manager so we can be enabled/disabled
		agentID = AIManager.GetInstance().AddAgent(this);
		//try to target a player if no other target's set
		if(Target == null)
		{
			Target = GameObject.FindWithTag("Player").GetComponent<PlayerLogic>().Actor;
		}
		setActor(Actor);
		healthInfo = actorCtrl.GetComponent<HealthInfo>();
		//to avoid surprising the player in debug mode, match our enabled status to the AI manager's agent status
		enabled = AIManager.GetInstance().AgentsEnabled;
	}
	
	void Update () {
		if(actorCtrl.IsAlive)
		{
			stateChangeTimer += Time.deltaTime;
			missileLaunchTimer += Time.deltaTime;
			//always track the target
			//if we're intercepting, however, this might be more complex
			trackTarget((enemyState != EnemyState.InterceptMissile) ? Target : interceptTarget);
			if(enemyState != EnemyState.InterceptMissile && targetInRange(Target, Mathf.Pow (AttackRange, 2)))
			{
				launchMissile();
			}
			updateState();
			trackMissiles();
		}
		else
		{
			//we must be dead; the actor's controller should play an animation. When the animation's done, drop loot and then remove the actor
			if(!actorCtrl.IsPlayingAnimation)
			{
				if(Drops != null && Actor != null) //kludge, figure out why Actor is still being accessed (probably because this component still exists despite actor ceasing to) and fix it
				{
					foreach(GameObject drop in Drops)
					{
						Instantiate(drop, Actor.transform.position, Actor.transform.rotation);
					}
				}
				
				//gameObject.SetActive(false);
				
				//Destroy(Actor);
				this.enabled = false;
				//remember to remove us from the manager to avoid calling a missing reference!
				AIManager.GetInstance().RemoveAgent(agentID);
				Destroy(gameObject);
				Destroy(this);
			}
		}
	}
	
	private void updateState()
	{
		switch (enemyState)
		{
			case EnemyState.Normal:
				if(targetInRange(Target, sqrGoToTrackDist))
				{
					enemyState = EnemyState.Track;
					break;
				}
				if(targetInRange(Target, sqrGoToStrafeDist))
				{
					enemyState = EnemyState.Strafe;
					break;
				}
				//randomly move towards the player
				if(Random.Range(0, 1+1) == 0)
				{
					//actorCtrl.Move(Vector3.forward);
					//Debug.Log("forward = " + Vector3.forward + ", moving by = " + (Actor.transform.position - Target.transform.position).normalized);
					Vector3 moveVec = new Vector3(0.0f, Mathf.Clamp(Target.transform.position.y - Actor.transform.position.y, -999.0f, 999.0f), 1.0f).normalized;
					actorCtrl.Move(moveVec);
				}
				break;
			case EnemyState.Track:
				if(targetInRange(Target, sqrGoToNormalDist))
				{
					enemyState = EnemyState.Normal;
					break;
				}
				//trackTarget(Target);
				break;
			case EnemyState.Strafe:
				actorCtrl.Move(Vector3.right);
				if(stateChangeTimer >= stateChangeTimeLimit)
				{
					if(Random.Range(0, 1+1) == 0)
					{
						enemyState = EnemyState.Normal;
						stateChangeTimer = 0;
						break;
					}
					if(Random.Range(0, 1+1) == 0)
					{
						enemyState = EnemyState.Backpedal;
						stateChangeTimer = 0;
						break;
					}
				}
				break;
			case EnemyState.Backpedal:
				actorCtrl.Move(Vector3.back);
				if(Random.Range(0, 1+1) == 0)
				{
					enemyState = EnemyState.Normal;
					break;
				}
				if(Random.Range(0, 1+1) == 0)
				{
					enemyState = EnemyState.Strafe;
					break;
				}
				break;
			case EnemyState.InterceptMissile:
				//evade the missiles and intercept when possible!
				if(Random.Range(0, 1+1) == 0 && stateChangeTimer >= stateChangeTimeLimit)
				{
					//jink to get some more room
					actorCtrl.Move(Vector3.right * Random.Range(-1, 1+1));
					stateChangeTimer = 0;
				}
				else
				{
					//run away!
					actorCtrl.Move(Vector3.back);
				}
				//shoot at the missile
				fireGuns(interceptTarget);
				break;
			default:
				break;
		}
		//cap our velocity if needed
		Actor.rigidbody.velocity = Vector3.ClampMagnitude(Actor.rigidbody.velocity, Speed);
	}
	
	private void launchMissile()
	{
		if(missileLaunchTimer >= missileFireRate && Target != null)
		{
			//launch a missile
			Quaternion targetAim = Quaternion.LookRotation(Target.transform.position - actorCtrl.transform.position);
			actorCtrl.FireMissile(Target.transform, targetAim);
			missileLaunchTimer = 0;
			//actorCtrl.RotateToOrientation(Quaternion.Slerp(Actor.transform.rotation, targetAim, TrackingSpeed));
		}
	}

	void fireGuns (GameObject interceptTarget)
	{
		if(interceptTarget != null)
		{
			//just like launchMissile(), but with no delay
			//to make things a little fairer, have the guns aim at an infinite distance from the missile
			Vector3 target = interceptTarget.rigidbody.position - Actor.rigidbody.position;
			target = target.normalized * actorCtrl.MaxLockOnDistance;
			Vector3 targetPos = Actor.rigidbody.position + target;
			actorCtrl.FireGuns(targetPos);
			//actorCtrl.RotateToOrientation(Quaternion.Slerp(Actor.transform.rotation, Quaternion.LookRotation(targetPos), TrackingSpeed));
		}
	}
	
	private void trackTarget(GameObject target)
	{
		if(target != null)
		{
			//compose a vector that won't cause the actor to rotate along x or z
			Vector3 targetVec = new Vector3(target.transform.position.x, 
											Actor.transform.position.y, 
											target.transform.position.z);
			//from that vector to the target, create a look at rotation to the vector, and set the actor's rotation to that rotation
			//Actor.transform.rotation = Quaternion.Slerp(Actor.transform.rotation, Quaternion.LookRotation(targetVec - Actor.transform.position), TrackingSpeed);
			actorCtrl.RotateToOrientation(Quaternion.Slerp(Actor.transform.rotation, Quaternion.LookRotation(targetVec - Actor.transform.position), TrackingSpeed));
		}
	}
	
	private bool targetInRange(GameObject target, float sqrDistance)
	{
		if(target != null)
		{
			return Vector3.SqrMagnitude(target.transform.position - Actor.transform.position) <= sqrDistance;
		}
		else
		{
			return false;
		}
	}

	private void trackMissiles()
	{
		//first, see if there's any missiles homing to this actor
		missiles = OrdinanceManager.GetMissilesTrackingTarget(Actor);
		//invalidate missile statistics
		sqrAvgMissileDist = -1.0f;
		numMissilesToIntercept = missiles.Count;
		if(missiles.Count > 0)
		{
			sqrAvgMissileDist = 0.0f;
			//we want to find the closest missile to us; build a distance list and order it by range
			//TODO: do the whole "order by range" part
			Dictionary<int, float> sqrDistsToMssls = new Dictionary<int, float>();
			missiles = OrdinanceManager.GetMissilesTrackingTarget(Actor);
			foreach(GameObject mssl in missiles.Values)
			{
				if(mssl != null)
				{
					float mag = Vector3.SqrMagnitude(mssl.transform.position - Actor.transform.position);
					sqrDistsToMssls[mssl.GetInstanceID()] = mag;
					sqrAvgMissileDist += mag;
				}
			}
			sqrAvgMissileDist /= missiles.Count;
			//pick the first missile in the sorted list
			var closestMsslID = sqrDistsToMssls.First().Key;
			//	(from dist in sqrDistsToMssls
			//	 orderby dist.Value //ascending keyword doesn't work!?
			//	 select dist.Key).First();
			//now intercept that missile!
			interceptTarget = OrdinanceManager.FindMissileByTargetAndID(Actor, closestMsslID);
			enemyState = EnemyState.InterceptMissile;
		}
		//if there's no more missiles, go back to doing something else
		else if(enemyState == EnemyState.InterceptMissile)
		{
			enemyState = EnemyState.Normal;
		}
	}

	void setActor (GameObject actor)
	{
		if(Actor != null)
		{
			actorCtrl = Actor.GetComponent<ActorController>();
			processor = Actor.GetComponentInChildren<AnimationProcessor>();
			Actor.transform.parent = transform;
			
			//set the actor's weapons to have infinite ammo
			actorCtrl.MissileMaxAmmo = -1;
			actorCtrl.CannonMaxAmmo = -1;
			Debug.Log("EnemyInput: ammo set");
		}
	}
	
	void setTarget(GameObject newTarget)
	{
		//generally pretty simple, but if the target's a valid player, then it sets its target to the player's actor
		PlayerLogic player = newTarget.GetComponentInChildren<PlayerLogic>();
		if(player != null)
		{
			Debug.Log("found player's actor");
			Target = player.Actor;
		}
		else
		{
			Target = newTarget;
		}
	}
	
	
}
