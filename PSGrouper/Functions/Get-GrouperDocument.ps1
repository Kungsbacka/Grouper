<#
    .SYNOPSIS
        Gets one or more Grouper documents.

    .DESCRIPTION
        Gets one or more Grouper documents from the Grouper database. A document entry
        contains the grouper document and some additional information like when the document
        was created and current revision number.

    .PARAMETER All
        Gets all documents from the database.

    .PARAMETER DocumentId
        Gets documents by document ID. This should always return one document only.
        More than one document could indicate that there are inconsitiencies in the Grouper database.

    .PARAMETER GroupId
        Gets documents by group ID.

    .PARAMETER GroupName
        Get documents by group name. Wildcards can be used.

    .PARAMETER MemberSource
        Get documents by member source.

    .PARAMETER RuleName
        Used in combination with RuleValue to find documents by member rule.

    .PARAMETER RuleValue
        Used in combination with RuleName to find documents by member rule. Wildcards can be used.

    .PARAMETER Store
        Only return documents for specified group store

    .PARAMETER IncludeUnpublished
        Include entries for unpublished documents

    .PARAMETER IncludeDeleted
        Include entries for deleted documents

    .OUTPUTS
        Grouper documents ([object[]])

    .EXAMPLE
        Get-GrouperDocument -DocumentId 'eea41e99-fa93-4d89-82de-9137cf79fe11'

    .EXAMPLE
        Get-GrouperDocument -GroupName 'My*'

    .EXAMPLE
        Get-GrouperDocument -RuleName 'Klass' -RuleValue '9B'
#>
function Get-GrouperDocument
{
    [CmdletBinding(DefaultParameterSetName='GroupName')]
    param (
        [Parameter(Mandatory=$true,ParameterSetName='FetchAll')]
        [switch]$All,
        [Parameter(Mandatory=$true,ParameterSetName='FetchAllUnpublished')]
        [switch]$AllUnpublished,
        [Parameter(Mandatory=$true,ParameterSetName='FetchAllDeleted')]
        [switch]$AllDeleted,
        [Parameter(Mandatory=$true,ParameterSetName='DocumentId', ValueFromPipeline=$true)]
        [object]$DocumentId,
        [Parameter(Mandatory=$true,ParameterSetName='GroupId')]
        [guid]$GroupId,
        [Parameter(Mandatory=$true,ParameterSetName='GroupName', Position=0)]
        [string]$GroupName,
        [Parameter(Mandatory=$true,ParameterSetName='MemberSource')]
        [GrouperLib.Core.GroupMemberSources]$MemberSource,
        [Parameter(Mandatory=$true,ParameterSetName='RuleNameAndValue')]
        [string]$RuleName,
        [Parameter(Mandatory=$false,ParameterSetName='RuleNameAndValue')]
        [string]$RuleValue,
        [Parameter(Mandatory=$false,ParameterSetName='FetchAll')]
        [Parameter(Mandatory=$false,ParameterSetName='FetchAllUnpublished')]
        [Parameter(Mandatory=$false,ParameterSetName='FetchAllDeleted')]
        [Parameter(Mandatory=$false,ParameterSetName='DocumentId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupName')]
        [Parameter(Mandatory=$false,ParameterSetName='MemberSource')]
        [Parameter(Mandatory=$false,ParameterSetName='RuleNameAndValue')]
        [GrouperLib.Core.GroupStores]$Store,
        [Parameter(Mandatory=$false,ParameterSetName='FetchAll')]
        [Parameter(Mandatory=$false,ParameterSetName='DocumentId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupName')]
        [Parameter(Mandatory=$false,ParameterSetName='MemberSource')]
        [Parameter(Mandatory=$false,ParameterSetName='RuleNameAndValue')]
        [switch]$IncludeUnpublished,
        [Parameter(Mandatory=$false,ParameterSetName='FetchAll')]
        [Parameter(Mandatory=$false,ParameterSetName='DocumentId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupName')]
        [Parameter(Mandatory=$false,ParameterSetName='MemberSource')]
        [Parameter(Mandatory=$false,ParameterSetName='RuleNameAndValue')]
        [switch]$IncludeDeleted,
        [Parameter(Mandatory=$false,ParameterSetName='FetchAll')]
        [Parameter(Mandatory=$false,ParameterSetName='FetchAllUnpublished')]
        [Parameter(Mandatory=$false,ParameterSetName='FetchAllDeleted')]
        [Parameter(Mandatory=$false,ParameterSetName='DocumentId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupId')]
        [Parameter(Mandatory=$false,ParameterSetName='GroupName')]
        [Parameter(Mandatory=$false,ParameterSetName='MemberSource')]
        [Parameter(Mandatory=$false,ParameterSetName='RuleNameAndValue')]
        [switch]$IncludeMetadata
    )

    begin {
        if (-not (CheckApi)) {
            break
        }
    }

    process {
        $defaultParams = @{
            store = $Store
            unpublished = $IncludeUnpublished
            deleted = $IncludeDeleted
        }
        switch ($PSCmdlet.ParameterSetName) {
            'FetchAll' {
                ApiGetDocuments 'all' $defaultParams $IncludeMetadata
                break
            }
            'FetchAllUnpublished' {
                ApiGetDocuments 'unpublished' @{store = $Store} $IncludeMetadata
                break
            }
            'FetchAllDeleted' {
                ApiGetDocuments 'deleted' @{store = $Store} $IncludeMetadata
                break
            }
            'DocumentId' {
                $docId = GetDocumentIdFromInputObject $DocumentId
                if ($null -eq $docId) {
                    return
                }
                ApiGetDocuments "id/$docId" $defaultParams $IncludeMetadata
                break
            }
            'GroupId' {
                ApiGetDocuments "group/id/$GroupId" $defaultParams $IncludeMetadata
                break
            }
            'GroupName' {
                ApiGetDocuments "group/name/$GroupName" $defaultParams $IncludeMetadata
                break
            }
            'MemberSource' {
                ApiGetDocuments "source/$MemberSource" $defaultParams $IncludeMetadata
                break
            }
            'RuleNameAndValue' {
                $fragment = "rule/$RuleName"
                if ($null -ne $RuleValue) {
                    $fragment += "/$RuleValue"
                }
                ApiGetDocuments $fragment $defaultParams $IncludeMetadata
                break
            }
        }
    }
}

Export-ModuleMember -Function 'Get-GrouperDocument'
