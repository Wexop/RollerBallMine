﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace RollerBallMine.Scripts;

public class RollerBallMine: NetworkBehaviour, IHittable
{
    private static readonly int Run = Animator.StringToHash("run");
    private static readonly int Open = Animator.StringToHash("open");
    public GameObject model;
    public Animator modelAnimator;
    public Animator spikeAnimator;
    public NavMeshAgent navMeshAgent;
    
    public AudioSource sfxAudioSource;
    public AudioSource runAudioSource;
    
    public AudioClip explodeClip;
    public AudioClip activateClip;
    public AudioClip runClip;
    
    public ParticleSystem explosion;
    public float speed = 5f;
    
    private float explosionRange = 5f;
    private int explosionDamage = 100;
    private float detectionRange = 10f;

    private bool detected;
    private bool hasExploded;
    private float explodeTime = 3f;
    private float detectPlayerRadius = 0.5f;

    public void SetValue(int value)
    {
        ScanNodeProperties scanNodeProperties = GetComponentInChildren<ScanNodeProperties>();
        if (scanNodeProperties != null)
        {
            scanNodeProperties.subText = $"Value: {value}";
        }
    }

    private void Start()
    {
        
        explodeTime = RollerBallMinePlugin.instance.ExplodeTime.Value;
        detectPlayerRadius = RollerBallMinePlugin.instance.TriggerRadius.Value;
        detectionRange = RollerBallMinePlugin.instance.DetectionRange.Value;
        explosionDamage = RollerBallMinePlugin.instance.ExplosionDamage.Value;
        speed = RollerBallMinePlugin.instance.Speed.Value;
        explosionRange = RollerBallMinePlugin.instance.ExplosionRange.Value;

        if (IsServer)
        {
            NetworkRollerBallMine.SetValueClientRpc( NetworkBehaviourId, Random.Range(40, 70));
        }
        navMeshAgent.enabled = true;
    }

    private void Update()
    {
        if (hasExploded) return;
        if (detected)
        {
            explodeTime -= Time.deltaTime;
            if (explodeTime <= 0)
            {
                if(IsServer) NetworkRollerBallMine.ExplodeClientRpc(NetworkObjectId);
            }
            navMeshAgent.Move(transform.forward * speed * Time.deltaTime);
            
            StartOfRound.Instance.allPlayerScripts.ToList().ForEach(player =>
            {
                if (Vector3.Distance(transform.position, player.transform.position) <= detectPlayerRadius)
                {
                    if(IsServer) NetworkRollerBallMine.ExplodeClientRpc(NetworkObjectId);
                }
            });
        }
        else
        {
            StartOfRound.Instance.allPlayerScripts.ToList().ForEach(player =>
            {
                if (Vector3.Distance(transform.position, player.transform.position) <= detectionRange && Vector3.Distance(new Vector3(0,transform.position.y,0)  , new Vector3(0,player.transform.position.y,0)) <= 2f  )
                {
                    if(IsServer) NetworkRollerBallMine.DetectPlayerClientRpc(NetworkObjectId, player.transform.position);
                }
            });
        }
        

    }

    public void DetectPlayer(Vector3 pos)
    {
        detected = true;
        transform.LookAt(pos);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        modelAnimator.SetBool(Run, true);
        spikeAnimator.SetBool(Open, true);
        

        
        sfxAudioSource.PlayOneShot(activateClip);
    }

    public void Explode()
    {
        if(hasExploded) return;
        
        hasExploded = true;
        explosion.Play();
        sfxAudioSource.PlayOneShot(explodeClip);
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position,
                transform.position) <= explosionRange)
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(explosionDamage);
        }

        List<EnemyAI> enemiesClose = FindObjectsOfType<EnemyAI>().ToList();
        enemiesClose.ForEach(enemy =>
        {
            if (Vector3.Distance(enemy.transform.position,
                    transform.position) <= explosionRange)
            {
                enemy.HitEnemy(5);
            }
        });
            
        List<Landmine> landminesClose = FindObjectsOfType<Landmine>().ToList();
        landminesClose.ForEach(mine =>
        {
            if ( !mine.hasExploded)
            {
                if (Vector3.Distance(mine.transform.position,
                        transform.position) <= explosionRange)
                {
                    if(IsServer) mine.ExplodeMineServerRpc();
                }
            }
        });
            
            
        StartCoroutine(DestroyObject());
    }
    
    public IEnumerator DestroyObject()
    {
        model.SetActive(false);
        yield return new WaitForSeconds(1f);
        if(IsServer) Destroy(gameObject);
    }
    
    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false,
        int hitID = -1)
    {
        if (IsServer) NetworkRollerBallMine.ExplodeClientRpc(NetworkObjectId);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Enemy") )
        {
            if (IsServer) NetworkRollerBallMine.ExplodeClientRpc(NetworkObjectId);
        }
    }
}