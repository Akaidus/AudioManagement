# Audio Management



This is still a WIP!



The Audio Management package contains scripts to easily setup an Audio Manager and through Audio Libraries play Audio Clips through Scriptable Objects by using enums.



Quick guide:

1. Create a Game Object and add the Audio Manager script to it. There should only ever exist one Audio Manager in the scene.

2. Create another Game Object and add the Pooled Audio Source script to it. Make this a prefab and drag the prefab into the Audio Source Prefab field on the Audio Manager.

3. Create a new Audio Library Scriptable Object and assign the desired Name (e.g. SoundEffect) and then press the "Add Library to Category Enum" button in the Inspector.
To create a new Audio Library, go to:
Assets -> Create -> Audio Management -> Audio Library

4. Create a new Audio Data Scriptable Object and in the Inspector assign the Name (e.g. CutePop), Audio Clips and the desired Volume.
To create a new Audio Data, go to:
Assets -> Create -> Audio Management -> Audio Data

5. Add the newly created Audio Data to the desired Audio Library's Audio Data list and press the "Re-generate Library Enum" button. This adds the Audio Data's name to the Audio Library's enum.
6. Add the newly created Audio Library to the Audio Manager's Audio Library Scriptable Objects array.

7. The setup is now done and the clip can now be played either from the Inspector of Audio Data Scriptable Object or through code by using a static Play method from the Audio Manager, e.g. AudioManager.Play(SoundEffect.Pop)
All AudioManager methods are displayed in the AudioPlayer script.

