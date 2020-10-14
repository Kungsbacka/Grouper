<#
    .SYNOPSIS
        Creates a new document

    .DESCRIPTION
        Creates a JSON document that can be converted to a Grouper document
        using ConvertTo-GrouperDocument.

    .PARAMETER GroupId
        Group GUID

    .PARAMETER GroupName
        Group name

    .PARAMETER Store
        Group store

    .INPUTS
        (none)

    .EXAMPLE
        New-GrouperDocument -GroupId '449e9f05-d939-9fbc-a110-1e8b49de9a91' -GroupName 'MyGroup' -Store AzureAD | Edit-GrouperDocument | Import-GrouperDocument

    .NOTES
        This is a prototype version of the cmdlet. A template member object is added that has to be edited by hand.
        The final version will take an array of member objects to create a complete Grouper document.
#>
function New-GrouperDocument
{
    param (
        [Parameter(Mandatory=$true)]
        [guid]
        $GroupId,
        [Parameter(Mandatory=$true)]
        [string]
        $GroupName,
        [Parameter(Mandatory=$true)]
        [GrouperLib.Core.GroupStores]
        $Store
    )

    process {
        $template = @'
{
  "id": "[DOC_ID]",
  "groupId": "[GROUP_ID]",
  "groupName": "[GROUP_NAME]",
  "store": "[STORE]",[OWNER_ACTION]
  "members": [
    {
      "source": "Static",
      "action": "Include",
      "rules": [
        {
        "name": "Upn",
        "value": "none@kungsbacka.se"
        }
      ]
    }
  ]
}
'@
        $ownerAction = ''
        if ($Store -eq 'AzureAD') {
            $ownerAction = "`r`n  `"owner`": `"AddAll`","
        }
        $json = $template
        $json = $json.Replace('[DOC_ID]', ([guid]::NewGuid().ToString()))
        $json = $json.Replace('[GROUP_ID]', $GroupId)
        $json = $json.Replace('[GROUP_NAME]', $GroupName)
        $json = $json.Replace('[STORE]', $Store)
        $json = $json.Replace('[OWNER_ACTION]', $ownerAction)
        [GrouperLib.Core.GrouperDocument]::FromJson($json)
    }
}

Export-ModuleMember -Function 'New-GrouperDocument'
