﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GUI_Vendor : NetTab
{
	[SerializeField]
	private bool allowSell = true;
	[SerializeField]
	private float cooldownTimer = 2f;
	[SerializeField]
	private string interactionMessage = "Item given.";
	[SerializeField]
	private string deniedMessage = "Bzzt.";
	public bool EjectObjects = false;
	[SerializeField]
	private EjectDirection ejectDirection = EjectDirection.None;

	private VendorTrigger vendor;
	private List<VendorItem> vendorContent = new List<VendorItem>();
	[SerializeField]
	private EmptyItemList itemList;

	private void Start()
	{
		vendor = Provider.GetComponent<VendorTrigger>();
		vendorContent = vendor.VendorContent;
		GenerateList();
	}

	protected override void InitServer()
	{
		
	}

	private void GenerateList()
	{
		itemList.Clear();
		itemList.AddItems(vendorContent.Count);
		for (int i = 0; i < vendorContent.Count; i++)
		{
			VendorItemEntry item = itemList.Entries[i] as VendorItemEntry;
			item.SetItem(vendorContent[i], this);
		}
	}

	public void VendItem(VendorItem item)
	{
		if (CanSell() == false)
			return;

		Vector3 spawnPos = vendor.gameObject.RegisterTile().WorldPositionServer;
		var spawnedItem = PoolManager.PoolNetworkInstantiate(item.item, spawnPos, vendor.transform.parent);

		//Ejecting in direction
		if (EjectObjects && ejectDirection != EjectDirection.None)
		{
			Vector3 offset = Vector3.zero;
			switch (ejectDirection)
			{
				case EjectDirection.Up:
					offset = vendor.transform.rotation * Vector3.up / Random.Range(4, 12);
					break;
				case EjectDirection.Down:
					offset = vendor.transform.rotation * Vector3.down / Random.Range(4, 12);
					break;
				case EjectDirection.Random:
					offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0);
					break;
			}
			spawnedItem.GetComponent<CustomNetTransform>()?.Throw(new ThrowInfo
			{
				ThrownBy = gameObject,
				Aim = BodyPartType.Chest,
				OriginPos = spawnPos,
				TargetPos = spawnPos + offset,
				SpinMode = ejectDirection == EjectDirection.Random ? SpinMode.Clockwise : SpinMode.None
			});
		}

		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	private bool CanSell()
	{
		if (!allowSell && deniedMessage != null && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UpdateChatMessage.Send(vendor.Originator, ChatChannel.Examine, deniedMessage);
		}
		else if (allowSell && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UpdateChatMessage.Send(vendor.Originator, ChatChannel.Examine, interactionMessage);
			return true;
		}
		return false;
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return WaitFor.Seconds(cooldownTimer);
		allowSell = true;
	}
}
