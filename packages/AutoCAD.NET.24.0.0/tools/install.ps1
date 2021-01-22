param($installPath, $toolsPath, $package, $project)
foreach ($reference in $project.Object.References)
{
    if($reference.Name -eq "accui" -Or
	   $reference.Name -eq "acdx" -Or
	   $reference.Name -eq "acmgd" -Or
	   $reference.Name -eq "acmr" -Or
	   $reference.Name -eq "acseamless" -Or
	   $reference.Name -eq "actcmgd" -Or
	   $reference.Name -eq "acwindows" -Or
	   $reference.Name -eq "adwindows" -Or
	   $reference.Name -eq "aduimgd" -Or
	   $reference.Name -eq "aduipalettes") 
	{
		$reference.CopyLocal = $false;
	}
}
