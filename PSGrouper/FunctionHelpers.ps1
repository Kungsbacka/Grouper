function GetDocumentIdFromInputObject($object)
{
    $documentId = [guid]::Empty
    if ($object -is [GrouperLib.Core.GrouperDocument]) {
        $null = [guid]::TryParse($object.Id, [ref]$documentId)
    }
    elseif ($object -is [string]) {
        $null = [guid]::TryParse($object, [ref]$documentId)
    }
    elseif ($object -is [guid]) {
        $documentId = $object
    }
    if ($object -is [psobject] -and $object.Document -is [GrouperLib.Core.GrouperDocument]) {
        $documentId = $object.Document.Id
    }
    if ($documentId -eq [guid]::Empty) {
        if (-not $NoThrow) {
            throw 'Could not convert input object to a document ID'
        }
        return
    }
    $documentId
}

function GetDocumentFromInputObject($object)
{
    if ($object -is [GrouperLib.Core.GrouperDocument]) {
        $document = $object
    }
    elseif ($object -is [psobject] -and $object.Document -is [GrouperLib.Core.GrouperDocument]) {
        $document = $object.Document
    }
    else {
        throw 'Could not convert input object to document JSON'
        return
    }
    $document
}
