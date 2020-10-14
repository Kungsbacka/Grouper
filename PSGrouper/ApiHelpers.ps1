function CheckApi()
{
    if (-not $Script:ApiUrl) {
        throw 'Not connected. Call Connect-GrouperApi before calling any other cmdlets.'
    }
    $true
}

function AddUrlParameter($url, $name, $value)
{
    $param = [System.Web.HttpUtility]::UrlEncode($name) + '=' + [System.Web.HttpUtility]::UrlEncode($value.ToString())
    if ($url.IndexOf('?') -gt 0) {
        "$url&$param"
    }
    else {
        "$url`?$param"
    }
}

function AddUrlParameters($url, $params)
{
    foreach ($param in $params.GetEnumerator()) {
        if ($null -ne $param.Value) {
            $url = AddUrlParameter $url $param.Key $param.Value
        }
    }
    $url
}

function GetApiUrl($controller, $fragment)
{
    $controller = $controller.Trim('/')
    if ($fragment) {
        "$($Script:ApiUrl.TrimEnd('/'))/$controller/$($fragment.TrimStart('/'))"
    }
    else {
        "$($Script:ApiUrl.TrimEnd('/'))/$controller"
    }
}

function ApiInvokeWebRequest($url, $method, $body)
{
    $params = @{
        Uri = $url
        Method = $method
        UseDefaultCredentials = $true
        UseBasicParsing = $true
    }
    if ($body) {
        if ($body -is [GrouperLib.Core.GrouperDocument]) {
            $params.Body = $body.ToJson('None')
        }
        else {
            $params.Body = $body
        }
        $params.ContentType = 'application/json; charset=utf-8'
    }
    $null = Invoke-WebRequest @params
}

function ApiGetDocuments($fragment, $params, $includeMeta)
{
    $url = GetApiUrl 'document' $fragment
    if ($params) {
        $url = AddUrlParameters $url $params
    }
    $entries = Invoke-RestMethod -Uri $url -Method 'Get' -UseDefaultCredentials -UseBasicParsing
    foreach ($entry in $entries) {
        $json = ConvertTo-Json -InputObject $entry.document -Depth 5 -Compress
        $grouperDocument = [GrouperLib.Core.GrouperDocument]::FromJson($json)
        if ($includeMeta) {
            [pscustomobject]@{
                Document = $grouperDocument
                GroupId = $entry.groupId
                GroupName = $entry.groupName
                Revision = $entry.revision
                RevisionCreated = $entry.revisionCreated
                IsPublished = $entry.isPublished
                IsDeleted = $entry.isDeleted
                Tags = $entry.tags
            }
        }
        else {
            $grouperDocument   
        }
    }
}

function ApiPostDocument($url, $doc)
{
    if ($doc -is [GrouperLib.Core.GrouperDocument]) {
        $json = $doc.ToJson('None')
    }
    else {
        $json = $doc
    }
    $params = @{
        Uri = $url
        Method = 'Post'
        Body = $json
        ContentType = 'application/json; charset=utf-8'
        UseDefaultCredentials = $true
        UseBasicParsing = $true
    }
    Invoke-RestMethod @params
}

function ApiGetLogItems($url, $params)
{
    if ($params) {
        $url = AddUrlParameters $url $params
    }
    Invoke-RestMethod -Uri $url -Method 'Get' -UseDefaultCredentials -UseBasicParsing
}
