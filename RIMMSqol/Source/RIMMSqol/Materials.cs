/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.05.2017
 * Time: 18:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using UnityEngine;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Materials.
	/// </summary>
	[StaticConstructorOnStartup]
	static public class Materials
	{
		public static readonly Material matLevelUp;
		public static readonly Material matLevelDown;
		public static readonly Texture2D iconPassionNone;
		public static readonly Texture2D iconPassionMinor;
		public static readonly Texture2D iconPassionMajor;
		public static readonly Texture2D iconOpinionBad;
		public static readonly Texture2D iconOpinionNeutral;
		public static readonly Texture2D iconOpinionGood;
		public static readonly Texture2D iconAlien;
		public static readonly Texture2D iconGenderMale, iconGenderFemale, iconGenderGenderless, iconPrisoner, iconRapport;
		public static readonly Texture2D designatorPathfinderFillDirections, designatorPathfinderDirections, 
		designatorPathfinderFillDirectionsN, designatorPathfinderFillDirectionsNE, designatorPathfinderFillDirectionsE, designatorPathfinderFillDirectionsSE, designatorPathfinderFillDirectionsS,
		designatorPathfinderFillDirectionsSW, designatorPathfinderFillDirectionsW, designatorPathfinderFillDirectionsNW, designatorPathfinderFillDirectionsCenter;
		public static readonly Material PowerBarFilledMat;
		public static readonly Material PowerBarUnfilledMat;
		public static readonly Texture2D iconEdit, iconWarning;
		public static readonly Texture2D backButtonSmall, dropButtonSmall, selectButtonSmall;
		public static readonly Texture2D badTexture;
		public static readonly Texture2D iconDice;
		public static readonly Texture2D iconUnforbid, iconForbid;
		
		public static readonly Texture2D skillShooting, skillMelee, skillSocial, skillAnimals, skillMedicine, 
			skillCooking, skillConstruction, skillGrowing, skillMining, skillArtistic, skillCrafting, skillIntellectual;
		public static readonly Dictionary<string,Texture2D> IconForSkill;
		
		static Materials()
		{
			matLevelUp = MaterialPool.MatFrom("Symbols/LevelUp", ShaderDatabase.MetaOverlay);
			matLevelDown = MaterialPool.MatFrom("Symbols/LevelDown", ShaderDatabase.MetaOverlay);
			iconPassionNone = ContentFinder<Texture2D>.Get("Symbols/PassionNone", true);
			iconPassionMinor = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor", true);
			iconPassionMajor = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor", true);
			PowerBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f), false);
			PowerBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f), false);
			iconEdit = ContentFinder<Texture2D>.Get("Symbols/Edit", true);
			backButtonSmall = ContentFinder<Texture2D>.Get("Symbols/BackButtonSmall", true);
			dropButtonSmall = ContentFinder<Texture2D>.Get("Symbols/DropButtonSmall", true);
			selectButtonSmall = ContentFinder<Texture2D>.Get("Symbols/SelectButtonSmall", true);
			badTexture = ContentFinder<Texture2D>.Get("UI/misc/BadTexture", true);
			iconAlien = ContentFinder<Texture2D>.Get("Symbols/alien", true);
			iconOpinionBad = ContentFinder<Texture2D>.Get("things/mote/thoughtsymbol/GenericBad", true);
			iconOpinionNeutral = ContentFinder<Texture2D>.Get("things/mote/speechsymbols/KindWords", true);
			iconOpinionGood = ContentFinder<Texture2D>.Get("things/mote/thoughtsymbol/GenericGood", true);
			designatorPathfinderFillDirections = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirections", true);
			designatorPathfinderDirections = ContentFinder<Texture2D>.Get("ui/designators/PathfinderDirections", true);
			designatorPathfinderFillDirectionsN = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsN", true);
			designatorPathfinderFillDirectionsNE = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsNE", true);
			designatorPathfinderFillDirectionsE = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsE", true);
			designatorPathfinderFillDirectionsSE = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsSE", true);
			designatorPathfinderFillDirectionsS = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsS", true);
			designatorPathfinderFillDirectionsSW = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsSW", true);
			designatorPathfinderFillDirectionsW = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsW", true);
			designatorPathfinderFillDirectionsNW = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsNW", true);
			designatorPathfinderFillDirectionsCenter = ContentFinder<Texture2D>.Get("ui/designators/PathfinderFillDirectionsCenter", true);
			iconWarning = ContentFinder<Texture2D>.Get("Symbols/Warning", true);
			iconGenderMale = ContentFinder<Texture2D>.Get("UI/Icons/gender/Male", true);
			iconGenderFemale = ContentFinder<Texture2D>.Get("UI/Icons/gender/Female", true);
			iconGenderGenderless = ContentFinder<Texture2D>.Get("UI/Icons/gender/Genderless", true);
			iconPrisoner = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners", true);
			iconRapport = ContentFinder<Texture2D>.Get("things/mote/speechsymbols/BuildRapport", true);
			iconDice = ContentFinder<Texture2D>.Get("Symbols/Dice", true);
			iconUnforbid = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff", true);
			iconForbid = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn", true);
			
			skillShooting = ContentFinder<Texture2D>.Get("Symbols/SkillShooting", true);
			skillMelee = ContentFinder<Texture2D>.Get("Symbols/SkillMelee", true);
			skillSocial = ContentFinder<Texture2D>.Get("Symbols/SkillSocial", true);
			skillAnimals = ContentFinder<Texture2D>.Get("Symbols/SkillAnimals", true);
			skillMedicine = ContentFinder<Texture2D>.Get("Symbols/SkillMedicine", true);
			skillCooking = ContentFinder<Texture2D>.Get("Symbols/SkillCooking", true);
			skillConstruction = ContentFinder<Texture2D>.Get("Symbols/SkillConstruction", true);
			skillGrowing = ContentFinder<Texture2D>.Get("Symbols/SkillGrowing", true);
			skillMining = ContentFinder<Texture2D>.Get("Symbols/SkillMining", true);
			skillArtistic = ContentFinder<Texture2D>.Get("Symbols/SkillArtistic", true);
			skillCrafting = ContentFinder<Texture2D>.Get("Symbols/SkillCrafting", true);
			skillIntellectual = ContentFinder<Texture2D>.Get("Symbols/SkillIntellectual", true);
			IconForSkill = new Dictionary<string, Texture2D>();
			IconForSkill.Add(SkillDefOf.Shooting.defName,skillShooting);
			IconForSkill.Add(SkillDefOf.Melee.defName,skillMelee);
			IconForSkill.Add(SkillDefOf.Social.defName,skillSocial);
			IconForSkill.Add(SkillDefOf.Animals.defName,skillAnimals);
			IconForSkill.Add(SkillDefOf.Medicine.defName,skillMedicine);
			IconForSkill.Add(SkillDefOf.Cooking.defName,skillCooking);
			IconForSkill.Add(SkillDefOf.Construction.defName,skillConstruction);
			IconForSkill.Add(SkillDefOf.Plants.defName,skillGrowing);
			IconForSkill.Add(SkillDefOf.Mining.defName,skillMining);
			IconForSkill.Add(SkillDefOf.Artistic.defName,skillArtistic);
			IconForSkill.Add(SkillDefOf.Crafting.defName,skillCrafting);
			IconForSkill.Add(SkillDefOf.Intellectual.defName,skillIntellectual);
		}
	}
}
