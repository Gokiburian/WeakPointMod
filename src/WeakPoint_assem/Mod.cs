using System;
using Modding;
using Modding.Blocks;
using UnityEngine;

namespace WeakPointSpace
{
	public class Mod : ModEntryPoint
	{
		GameObject mod;

		public override void OnLoad()
		{
			// Called when the mod is loaded.
			Debug.Log("WeakPoint Loaded! (>_<)");

			mod = new GameObject("WeakPointController");
			SingleInstance<BlockSelector>.Instance.transform.parent = mod.transform;

			// �V�[�����܂����ł�mod��������Ȃ��悤�ɂ���B
			UnityEngine.Object.DontDestroyOnLoad(mod);

			EffectsSpawner.OnLoad();
			//BlockDeleter.OnLoad();
		}

	}

	public class BlockSelector : SingleInstance<BlockSelector>
	{
		public override string Name
		{
			get
			{
				return "WeakPoint BlockSelector";
			}
		}

		// ���\�b�h
		public void Awake()
		{
			// �u���b�N��ݒu�����ꍇ�ɌĂяo�����A�N�V�����ɁAAddScript�Ƃ������\�b�h��ǉ�����
			Events.OnBlockInit += new Action<Block>(AddScript);
		}

		// �u���b�N�ݒu���ɁA���̃u���b�N�ɏ���̃X�N���v�g��\��t����֐�
		// Block�́A�ݒu�����u���b�N��\��
		public void AddScript(Block block)
		{
			// ���������u���b�N��BlockBehaviour�R���|�[�l���g���擾����
			BlockBehaviour internalObject = block.BuildingBlock.InternalObject;

			if (internalObject.BlockID != 7 && internalObject.BlockID != 45 
				&& internalObject.BlockID != 75 && internalObject.BlockID != 57 && internalObject.BlockID != 58)
			{

				try
				{
					// �܂�����̃X�N���v�g���\��t�����Ă��Ȃ��ꍇ�ɂ̂݁A�\��t����
					if (internalObject.GetComponent(typeof(WeakPointScript)) == null)
					{
						WeakPointScript weakpoint = internalObject.gameObject.AddComponent<WeakPointScript>();
						Debug.Log("WeakPoint : Added Script");
					}
				}
				catch
				{
					Debug.LogError("WeakPoint : AddScript Error!");
				}
				return;
			}

		}
	}

	public class WeakPointScript : MonoBehaviour
	{
		// �ϐ�
		public BlockBehaviour BB;
		private MToggle shockDetection;
		private MSlider thresholdSlider;
		private MMenu effectMenu;

		private bool isInit;

		private float waittime;

		private MessageType effectmessageType;
		private Message effectmessage;

		/*
		private MToggle shockDisapper;
		private MessageType deletemessageType;
		private Message deletemessage;
		*/

		/*
		private Renderer[] componentsInChildren;
		private Color[] defaultColor;
		private bool isChanged;
		*/

		// ���\�b�h
		private void Awake()
		{
			// BlockBehaviour���擾"
			BB = GetComponent<BlockBehaviour>();

			//�ݒ�̓o�^
			shockDetection = BB.AddToggle("Shock Detection", "Shock Detection", false);
			//shockDisapper = BB.AddToggle("Disapper on Shock", "Disapper on Shock", false);
			thresholdSlider = BB.AddSlider("Shock Toughness", "Shock Toughness", 1, 0, 10);
			effectMenu = BB.AddMenu("Schock Effect", 0, new System.Collections.Generic.List<string>() { "Shock type A", "Shock type B", "Shock type C" });
			//�ݒ�̕\����shockDetection�ˑ���
			shockDetection.Toggled += SetActive;
			//�G�t�F�N�g��ς��邽�уG�t�F�N�g����
			isInit = false;
			effectMenu.ValueChanged += SpawnSampleEffect;

			//�A���q�b�g�h�~
			waittime = 0f;

			//�N���C�A���g�ɂ�����𑣂����߂̃��b�Z�[�W�̌^
			effectmessageType = EffectsSpawner.messageType;
			//deletemessageType = BlockDeleter.messageType;

			/*
			//�u���b�N�̐F�ւ�����
			componentsInChildren = base.transform.FindChild("Vis").GetComponentsInChildren<Renderer>();
			defaultColor = new Color[componentsInChildren.Length];
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				defaultColor[i] = componentsInChildren[i].material.color;
			}
			isChanged = false;
			*/
		}

		private void SetActive(bool isActive)
		{
			thresholdSlider.DisplayInMapper = isActive;
			effectMenu.DisplayInMapper = isActive;
			//shockDisapper.DisplayInMapper = isActive;
		}

		private void SpawnSampleEffect(int i)
        {
			if (!BB.isSimulating && isInit && shockDetection.IsActive)
			{
				effectmessage = effectmessageType.CreateMessage(BB.GetCenter(), effectMenu.Value);
				ModNetworking.SendToAll(effectmessage);
				EffectsSpawner.SpawnEffect(BB.GetCenter(), Quaternion.identity, i);

				//ChangeColor(true);
			}
            else
            {
				isInit = true;
            }
        }

		public void Update()
		{
			if (waittime > 0f)
			{
				waittime -= Time.deltaTime;

				/*
				if (waittime < 0.9f)
				{
					ChangeColor(false);
				}
				*/
			}
		}

		public void OnCollisionEnter(Collision collision)
		{
			bool isOverToughness = BB.isSimulating && shockDetection.IsActive && waittime <= 0f && collision.impulse.sqrMagnitude > thresholdSlider.Value * thresholdSlider.Value * 30f;

			if (isOverToughness)
			{
				waittime = 1f;

				//�N���C�A���g�łȂ����A���[�J���V�~���ł��邩�@�[���@�����ŃV�~�����Ă���Ƃ�
				if (!StatMaster.isClient || StatMaster.isLocalSim)
				{
					//�N���C�A���g�փ��b�Z�[�W�̑��M
					effectmessage = effectmessageType.CreateMessage(BB.GetCenter(), effectMenu.Value);
					ModNetworking.SendToAll(effectmessage);
					EffectsSpawner.SpawnEffect(effectmessage);

					//ChangeColor(true);

					switch (BB.Team)
					{
						case MPTeam.None:
							Debug.Log("WeakPoint : None Team shocked");
							ModTriggers.GetCallback(1)();
							break;
						case MPTeam.Red:
							Debug.Log("WeakPoint : Red Team shocked");
							ModTriggers.GetCallback(2)();
							break;
						case MPTeam.Green:
							Debug.Log("WeakPoint : Green Team shocked");
							ModTriggers.GetCallback(3)();
							break;
						case MPTeam.Orange:
							Debug.Log("WeakPoint : Orange Team shocked");
							ModTriggers.GetCallback(4)();
							break;
						case MPTeam.Blue:
							Debug.Log("WeakPoint : Blue Team shocked");
							ModTriggers.GetCallback(5)();
							break;
					}

					/*
                    if (shockDisapper.IsActive)
                    {
						deletemessage = deletemessageType.CreateMessage(this.gameObject);
						ModNetworking.SendToAll(deletemessage);
						BlockDeleter.Delete(deletemessage);
					}
					*/
				}
			}
		}

		/*
		private void ChangeColor(bool damage)
		{
			//����
			if (damage && !isChanged)
			{
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].material.color = new Color(2, 2, 2, 1);
				}
				isChanged = true;
			}
			//�߂�
			else if (!damage && isChanged)
			{
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].material.color = defaultColor[i];
				}
				isChanged = false;
			}
		}
		*/
	}

	public static class EffectsSpawner
	{
		public static GameObject HitEeffectA;
		public static GameObject HitEeffectB;
		public static GameObject HitEeffectC;

		public static MessageType messageType;

		public static void OnLoad()
		{
			//AssetBundle�̓ǂݍ���
			ModAssetBundle assetBundle = ModResource.GetAssetBundle("effects");
			HitEeffectA = assetBundle.LoadAsset<GameObject>("HitEffectA");
			HitEeffectB = assetBundle.LoadAsset<GameObject>("HitEffectB");
			HitEeffectC = assetBundle.LoadAsset<GameObject>("HitEffectC");

			Debug.Log("WeakPoint : Loading Effects");

			//�N���C�A���g�ɂ�����𑣂����߂̃��b�Z�[�W�̌^�Ǝ󗝎�����̓o�^
			messageType = ModNetworking.CreateMessageType(DataType.Vector3, DataType.Integer);
			ModNetworking.Callbacks[messageType] += new Action<Message>(SpawnEffect);
		}

		public static void SpawnEffect(Message message)
		{
			SpawnEffect((Vector3)message.GetData(0), Quaternion.identity, (int)message.GetData(1));
		}

		public static void SpawnEffect(Vector3 position, Quaternion rotation, int effecttype)
		{
			GameObject hiteffect = HitEeffectA;
			switch (effecttype)
			{
				case 0:
					hiteffect = HitEeffectA;
					break;
				case 1:
					hiteffect = HitEeffectB;
					break;
				case 2:
					hiteffect = HitEeffectC;
					break;
			}
			//�G�t�F�N�g�̐����ƍ폜
			UnityEngine.Object obj = UnityEngine.Object.Instantiate((UnityEngine.Object)(object)hiteffect, position, rotation, ReferenceMaster.physicsGoalInstance);
			UnityEngine.Object.Destroy(obj, 10f);
		}
	}

	/*
	public static class BlockDeleter
	{
		public static MessageType messageType;
		public static void OnLoad()
        {
			messageType = ModNetworking.CreateMessageType(DataType.Block);
			ModNetworking.Callbacks[messageType] += new Action<Message>(Delete);
		}

		public static void Delete(Message message)
        {
			((GameObject)message.GetData(0)).SetActive(false);
        }
	}
	*/
}
