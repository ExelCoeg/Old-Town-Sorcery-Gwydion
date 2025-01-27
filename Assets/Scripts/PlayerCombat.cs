using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerCombat : MonoBehaviour
{
    /*------ GetComponents -------*/
    Rigidbody2D rb;
    SpellCaster spellCaster;
    Animator anim;

    /*---------- UI -------------*/
    [SerializeField] TextMeshProUGUI combatStateText;

    Vector2 mousePos;

    /*---------- spells -----------*/
    
    int currentSpell;
    public List<Spell> ownedSpells = new List<Spell>();

    /*---------- potions ----------*/
    [Header("Potions Attributes")]
    public int currentPotion;
    public int throwSpeed;
    public List<GameObject> ownedPotions = new List<GameObject>();

    /*---------- attack attributes --------*/
    [Header("Attack Attributes")]
    public Transform firePoint;
    public float attackRadius;
    public float attackDamage;
    public float maxAttackDamage;
    public bool isAttacking;

    /*---------- State Variables----------- */
    [Header("States")]

    int currentState = 0;
    [SerializeField] List<bool> onStates = new List<bool>();
    public bool onMelee = true;
    public bool onSpell = false;
    public bool onPotion = false;


    /*----- Inventory & item data ------*/

    public Inventory inventory;
    public ItemList itemList;
    /*
     onStates[0] = onMelee
     onStates[1] = onSpell
     onStates[2] = onPotion
      */

    /*---------- animation string variables ----------*/
    private string attack_parameter = "attack";
    private string playerCasting_parameter = "player_casting_spell";
    private string onBuffAttackPotion_parameter = "player_potion_buff_attack";
    private string onBuffAttackPotionWalk_parameter = "player_potion_buff_attack_walk";
    private string onHealPotion_parameter = "player_potion_heal";
    private string onHealPotionWalk_parameter = "player_potion_heal_walk";
    private string onDefenseDebuffPotion_parameter = "player_potion_defense_debuff";
    private string onDefenseDebuffPotionWalk_parameter = "player_potion_defense_debuff_walk";
    private string playerHealed_parameter = "player_healed";
    private string idle_parameter = "player_idle";
    private string currentAnimation;
    public bool onAnimation;
   /*------------- layers -----------*/
   public LayerMask enemyLayer;
   public LayerMask damagableLayer;
   
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spellCaster = GetComponent<SpellCaster>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        attackDamage = maxAttackDamage;

        onStates.Add(onMelee);
        onStates.Add(onSpell);
        onStates.Add(onPotion);
        
    }
    // Update is called once per frame
    void Update()
    {
        if(!GameManager.instance.pause){

            AimingAt();
            UpdateCurrentCombatStateText();
            PlayerAnimation();
            CycleState();
            
            if (onPotion)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    currentPotion--;
                    AudioManager.instance.PlaySFX("move_potion");
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentPotion++;
                    AudioManager.instance.PlaySFX("move_potion");
                }
                if (currentPotion < 0) currentPotion = 0;
                if(ownedPotions.Count == 0){
                    NextState();
                }
                if (currentPotion >= ownedPotions.Count) currentPotion = ownedPotions.Count - 1;
                if (Input.GetMouseButtonDown(0) && !onAnimation) 
                {
                    UsePotion(ownedPotions[currentPotion]);     
                }

                
            }

            if (onSpell)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    currentSpell--;
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentSpell++;
                }
                if (currentSpell < 0) currentSpell = 0;
                if (currentSpell >= ownedSpells.Count) currentSpell = ownedSpells.Count - 1;
                if (ownedSpells.Count > 0 )
                {
                    if (Input.GetMouseButtonDown(0) && GetComponent<PlayerMana>().currentMana >= ownedSpells[currentSpell].manaCost)
                    {
                        
                        if (ownedSpells[currentSpell].GetType().Name == "AOESpell")
                        {
                            spellCaster.AOECast(ownedSpells[currentSpell] as AOESpell, firePoint);
                        }
                        if(ownedSpells[currentSpell].GetType().Name == "SingleTargetSpell"){
                            
                            var spell = ownedSpells[currentSpell] as SingleTargetSpell;
                            spellCaster.SingleTargetCast(spell, spell.effect);
                        }
                        ChangeAnimation(playerCasting_parameter);
                        
                    }
                }
            }
            if(onMelee)
            {
                if (Input.GetMouseButtonDown(0) && !isAttacking){
                    PlayerAttack();
                } 
            }
        }
    }
    void PlayerAttack()
    {
        isAttacking = true;
    
        anim.SetTrigger(attack_parameter);
        Collider2D[] hits= Physics2D.OverlapCircleAll(firePoint.position, attackRadius,enemyLayer);
        if(hits.Length == 0){
            AudioManager.instance.PlaySFX("sword_attack");
        }
        else{
            AudioManager.instance.PlaySFX("sword_impact");
        }
        foreach(Collider2D hit in hits)
        {
            if (hit.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth)) 
            {
                enemyHealth.TakeDamage(attackDamage);
            }
            
        }  
        Collider2D[] hitsResource= Physics2D.OverlapCircleAll(firePoint.position, attackRadius,damagableLayer);
        foreach(Collider2D hitResource in hitsResource){
            if(hitResource.TryGetComponent<ResourceObjectHealth>(out ResourceObjectHealth resourceObject)) {
                print("test");
                resourceObject.TakeDamage(attackDamage);
                AudioManager.instance.PlaySFX("resource_destroy");
            }
        
        }
        
    }
    void AimingAt()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        Vector3 currentRotation = firePoint.localEulerAngles;
        
        if (GetComponent<PlayerMovement>().isFacingRight) currentRotation.z = angle;
        else currentRotation.z = -angle;
        firePoint.localEulerAngles = currentRotation;
    }
   
    void UpdateCurrentCombatStateText()
    {
        if (onStates[0]) combatStateText.text = "Using: Sword" ;
        if (onStates[1] && ownedSpells.Count > 0) combatStateText.text = "Spell: " + ownedSpells[currentSpell].spellName;
        if (onStates[2] && ownedPotions.Count > 0) combatStateText.text = "Potion: " + ownedPotions[currentPotion].name;
    }
    void ConsumePotion(GameObject potion){
        potion.GetComponent<IConsummable>().Use();
    }
    void ThrowPotion(GameObject potion)
    {
        GameObject potionClone = Instantiate(potion, firePoint.position, Quaternion.identity);
        if(potionClone.TryGetComponent<IThrowable>(out IThrowable throwable))
        {
           

            throwable.Launch(mousePos, throwSpeed);
        }
    }

    void UsePotion(GameObject potion)
    {
        
        var throwable = potion.GetComponent<ThrowablePotion>();
        var consummable = potion.GetComponent<ConsummablePotion>();
        if(throwable != null){
            ThrowPotion(potion);
        }
        
        if (consummable!= null){
            print("consume potion");

            AudioManager.instance.PlaySFX("potion_consume");
            ConsumePotion(potion);
            if(consummable.potionName == ConsummablePotionName.HEAL && !onAnimation) 
            {
                onAnimation = true;
                ChangeAnimation(playerHealed_parameter);
            }
        }
        ownedPotions.Remove(potion);
        if(potion.name == "Defense Debuff Potion"){
            inventory.Remove(itemList.defenseDebuffPotionData);
        }
        if(potion.name == "Healing Potion"){
            inventory.Remove(itemList.healingPotionData);
        }
        if(potion.name == "Buff Attack Potion"){
            inventory.Remove(itemList.buffATKPotionData);
        }
        currentPotion--;
        currentPotion = 0;
    }
    void PlayerAnimation()
    {
        if(onStates[0] && !onAnimation || ownedPotions.Count == 0)
        {
            ChangeAnimation(idle_parameter);
        }
        
        if(onStates[2] && ownedPotions.Count > 0 && !onAnimation){
            if(ownedPotions[currentPotion].name == "Defense Debuff Potion")
            {
                if(rb.velocity.x != 0){
                    ChangeAnimation(onDefenseDebuffPotionWalk_parameter);
                }
                else{
                    ChangeAnimation(onDefenseDebuffPotion_parameter);
                }
            } 
            if(ownedPotions[currentPotion].name =="Buff Attack Potion") 
            {
                if(rb.velocity.x != 0 ){
                    ChangeAnimation(onBuffAttackPotionWalk_parameter);
                }
                else{
                    ChangeAnimation(onBuffAttackPotion_parameter);
                }
            }
            if(ownedPotions[currentPotion].name == "Heal Potion")
            {
                if(rb.velocity.x != 0 ){
                    ChangeAnimation(onHealPotionWalk_parameter);
                }
                else{
                    ChangeAnimation(onHealPotion_parameter);
                }
            }
           
        }
        
    }
    void ChangeAnimation(string newAnimation)
    {
        if (currentAnimation == newAnimation) return;
        anim.Play(newAnimation);
        currentAnimation = newAnimation;
    }
    void CycleState()
    {
        if (Input.GetMouseButtonDown(1))
        {
            NextState();
        }
       
        onMelee = onStates[0];
        onSpell = onStates[1];
        onPotion = onStates[2];
    }
    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(firePoint.position, attackRadius);
    }

    void NextState(){
        onStates[currentState] = false;
        
        currentState++;
        if(currentState >= onStates.Count)
        {
            currentState = 0;
        }
        if(currentState == 0){
            AudioManager.instance.PlaySFX("sword_switch");
        }
        if(currentState == 1){
            AudioManager.instance.PlaySFX("magic_staff_switch");
        }
        if(currentState == 2){
            AudioManager.instance.PlaySFX("move_potion");
        }
        onStates[currentState] = true;
    }
}
