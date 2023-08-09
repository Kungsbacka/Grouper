USE [Grouper]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_document_tags] (
    @document_id uniqueidentifier,
    @single_row bit = 0
)
RETURNS @document_tags TABLE (
    tag nvarchar(MAX)
)
AS
BEGIN
    IF @single_row = 1
    BEGIN
        DECLARE @tags nvarchar(MAX);
        SELECT @tags = (SELECT tag + ', ' FROM dbo.document_tag WHERE document_id = @document_id FOR XML PATH(''));
        INSERT INTO @document_tags SELECT LEFT(@tags, LEN(@tags) - 1);
    END
    ELSE
    BEGIN
        INSERT INTO @document_tags SELECT tag FROM dbo.document_tag WHERE document_id = @document_id;
    END

    RETURN;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/* Used by SSRS reports to search the operational log */
CREATE FUNCTION [dbo].[fn_search_operational_log] (
    @search nvarchar(200) = NULL
)
RETURNS @log_entries TABLE (
	log_time datetime,
	document_id uniqueidentifier,
	group_id uniqueidentifier,
	group_display_name nvarchar(200),
	group_store nvarchar(30),
	target_id uniqueidentifier,
	target_display_name nvarchar(200),
	operation nvarchar(20)
)
AS
BEGIN
    SET @search = LTRIM(RTRIM(@search));

	INSERT INTO @log_entries SELECT TOP 1000
        log_time,
        document_id,
        group_id,
        group_display_name,
        group_store,
        target_id,
        target_display_name,
        operation
    FROM
        dbo.operational_log
    WHERE
        @search IS NULL
    OR
        @search = ''
    OR
        CHARINDEX(@search, document_id) > 0
    OR
        CHARINDEX(@search, group_id) > 0
    OR
        CHARINDEX(@search, group_display_name) > 0
    OR
        CHARINDEX(@search, target_display_name) > 0
    ORDER BY
        log_time DESC;

    RETURN;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[operational_log](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[log_time] [datetime] NOT NULL,
	[document_id] [uniqueidentifier] NOT NULL,
	[group_id] [uniqueidentifier] NOT NULL,
	[group_display_name] [nvarchar](200) NULL,
	[group_store] [nvarchar](30) NOT NULL,
	[operation] [nvarchar](20) NOT NULL,
	[target_id] [uniqueidentifier] NOT NULL,
	[target_display_name] [nvarchar](200) NULL,
 CONSTRAINT [PK_operational_log] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_operational_log] (
    @document_id [uniqueidentifier] = NULL,
    @group_id [uniqueidentifier] = NULL,
    @target_id [uniqueidentifier] = NULL,
    @operation [nvarchar](20) = NULL,
    @target_display_name_contains [nvarchar](200) = NULL,
    @group_display_name_contains [nvarchar](200) = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
)
RETURNS TABLE
AS
RETURN
    SELECT TOP (@count)
        log_time,
        document_id,
        group_id,
        group_display_name,
        group_store,
        target_id,
        target_display_name,
        operation
    FROM
        dbo.operational_log
    WHERE
        (@document_id IS NULL OR document_id = @document_id)
    AND
        (@group_id IS NULL OR group_id = @group_id)
    AND
        (@target_id IS NULL OR target_id = @target_id)
    AND
        (@operation IS NULL OR operation = @operation)
    AND
        (@start IS NULL OR log_time >= @start)
    AND
        (@end IS NULL OR log_time <= @end)
    AND
        (@target_display_name_contains IS NULL OR CHARINDEX(@target_display_name_contains, target_display_name) > 0)
    AND
        (@group_display_name_contains IS NULL OR CHARINDEX(@group_display_name_contains, group_display_name) > 0)
    ORDER BY
        log_time DESC;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[document](
	[document_id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[created] [datetime] NOT NULL,
	[published] [bit] NOT NULL,
	[deleted] [bit] NOT NULL,
	[document_json] [nvarchar](max) NOT NULL,
	[processing_interval]  AS (json_value([document_json],'$.interval')),
	[group_id]  AS (json_value([document_json],'$.groupId')),
	[group_store]  AS (json_value([document_json],'$.store')),
	[group_name]  AS (json_value([document_json],'$.groupName')),
	[owner_action]  AS (json_value([document_json],'$.owner')),
 CONSTRAINT [PK_document] PRIMARY KEY CLUSTERED 
(
	[document_id] ASC,
	[revision] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[latest_revision]
AS
SELECT
    a.document_id,
    a.revision,
    a.created,
    a.published,
    a.deleted,
    a.document_json,
    a.processing_interval,
    a.group_id,
    a.group_store,
    a.group_name,
    a.owner_action
FROM
    dbo.document a
WHERE
    a.revision = (SELECT MAX(revision) FROM dbo.document WHERE document_id = a.document_id)

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[document_tag](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[document_id] [uniqueidentifier] NOT NULL,
	[tag] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_document_tag] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_unpublished_documents] (
    @store nvarchar(50) = NULL
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        published = 0
    AND
        deleted = 0
    AND
        (@store IS NULL OR group_store = @store);
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE FUNCTION [dbo].[fn_get_document_by_age] (
    @start datetime,
    @end datetime = null,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = d.document_id) AS tags,
        document_json
    FROM
        dbo.document d
    WHERE
    (
            (@end IS NULL AND created >= @start)
        OR
            (created BETWEEN @start AND @end)
    )
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[fn_get_document_by_processing_interval] (
    @min int,
    @max int = 2147483647,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        processing_interval > 0
    AND
        processing_interval BETWEEN @min and @max
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[flattened_document]
AS
SELECT
    document_id,
    revision,
    created,
    published,
    deleted,
    JSON_VALUE(document_json, '$.groupId') AS group_id,
    JSON_VALUE(document_json, '$.groupName') AS group_name,
    JSON_VALUE(document_json, '$.store') AS group_store,
    JSON_VALUE(document_json, '$.owner') AS owner_action,
    CASE WHEN processing_interval IS NULL THEN 0 ELSE CAST(processing_interval AS int) END AS processing_interval,
    [member].[source] AS member_source,
    [member].[action] AS member_action,
    [rule].[name] AS rule_name,
    [rule].[value] AS rule_value,
    document_json
FROM
    dbo.document
CROSS APPLY
    OPENJSON(document_json, '$.members')
        WITH (
            [source] nvarchar(20) '$.source',
            [action] nvarchar(10) '$.action',
            [rules] nvarchar(MAX) '$.rules' AS JSON
        ) AS [member]
CROSS APPLY
    OPENJSON([member].[rules])
        WITH (
            [name] nvarchar(30) '$.name',
            [value] nvarchar(200) '$.value'
        ) AS [rule]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[latest_revision_flattened]
AS
SELECT
    document_id,
    revision,
    created,
    published,
    deleted,
    group_id,
    group_name,
    group_store,
    owner_action,
    processing_interval,
    member_source,
    member_action,
    rule_name,
    rule_value,
    document_json
FROM
    dbo.flattened_document a
WHERE
    a.revision = (SELECT MAX(revision) FROM dbo.document WHERE document_id = a.document_id)

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[fn_get_document_by_document_id] (
    @document_id uniqueidentifier,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        document_id = @document_id
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_all_documents] (
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );

    /* WHERE clause behavior
     *
     * P/U - published/unpublished
     * D/N - deleted/not deleted
     *
     * IU = 1 -> include unpublished
     * ID = 1 -> include deleted
     *
     *        | P+N   U+N    U+D    P+D
     *  ------+----------------------------------
     *  (none)| true  false  false  (illegal state)
     *  IU    | true  true   false  (illegal state)
     *  ID    | true  false  true   (illegal state)
     *  IU+ID | true  true   true   (illegal state)
     * 
     */
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[fn_get_all_documents_flattened] (
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0

)
RETURNS TABLE
AS
RETURN
    SELECT
       document_id
      ,revision
      ,created
      ,published
      ,deleted
      ,(SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags
      ,group_id
      ,group_name
      ,group_store
      ,owner_action
      ,member_source
      ,member_action
      ,rule_name
      ,rule_value
    FROM
        dbo.latest_revision_flattened r
    WHERE
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[audit_log](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[log_time] [datetime] NOT NULL,
	[document_id] [uniqueidentifier] NOT NULL,
	[actor] [nvarchar](50) NOT NULL,
	[action] [nvarchar](100) NOT NULL,
	[additional_information] [nvarchar](200) NULL,
 CONSTRAINT [PK_audit_log] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[fn_get_audit_log] (
    @document_id [uniqueidentifier] = NULL,
    @actor_contains [nvarchar](50) = NULL,
    @action_contains [nvarchar](100) = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
)
RETURNS TABLE
AS
RETURN
	SELECT TOP (@count)
        log_time,
        document_id,
        actor,
        [action],
        additional_information
    FROM
        dbo.audit_log
    WHERE
        (@document_id IS NULL OR document_id = @document_id)
    AND
        (@start IS NULL OR log_time >= @start)
    AND
        (@end IS NULL OR log_time <= @end)
    AND
        (@actor_contains IS NULL OR CHARINDEX(@actor_contains, actor) > 0)
    AND
        (@action_contains IS NULL OR CHARINDEX(@action_contains, [action]) > 0)
    ORDER BY
        log_time DESC;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_deleted_documents] (
    @store nvarchar(50) = NULL
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        deleted = 1
    AND
        (@store IS NULL OR group_store = @store);

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_document_by_group_id] (
    @group_id uniqueidentifier,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision r
    WHERE
        group_id = @group_id
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[fn_get_document_by_group_name] (
    @group_name nvarchar(200),
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = d.document_id) AS tags,
        document_json
    FROM
        dbo.document d
    WHERE
        group_name LIKE @group_name
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[fn_get_document_by_member_rule] (
    @rule_name nvarchar(100),
    @rule_value nvarchar(200) = NULL,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision_flattened r
    WHERE
        rule_name = @rule_name
    AND
        (rule_value LIKE @rule_value OR @rule_value IS NULL)
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE FUNCTION [dbo].[fn_get_document_by_member_source] (
    @source nvarchar(50),
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
)
RETURNS TABLE
AS
RETURN
    SELECT
        revision,
        created,
        published,
        deleted,
        (SELECT STRING_AGG(tag, ',') FROM dbo.document_tag WHERE document_id = r.document_id) AS tags,
        document_json
    FROM
        dbo.latest_revision_flattened r
    WHERE
        member_source = @source
    AND
        (@store IS NULL OR group_store = @store)
    AND
    (
            (deleted = 0 AND (published = 1 OR @include_unpublished = 1))
        OR
            (deleted = 1 AND @include_deleted = 1)
    );
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[event_log](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[log_time] [datetime] NOT NULL,
	[document_id] [uniqueidentifier] NULL,
	[group_id] [uniqueidentifier] NULL,
	[group_display_name] [nvarchar](200) NULL,
	[group_store] [nvarchar](30) NULL,
	[log_level] [tinyint] NOT NULL,
	[log_message] [nvarchar](2000) NOT NULL,
 CONSTRAINT [PK_error_log] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[fn_get_event_log] (
    @document_id [uniqueidentifier] = NULL,
    @group_id [uniqueidentifier] = NULL,
    @message_contains nvarchar(200) = NULL,
    @group_display_name_contains nvarchar(200) = NULL,
    @log_level [int] = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
)
RETURNS TABLE
AS
RETURN
	SELECT TOP (@count)
        log_time,
        document_id,
        group_id,
        group_display_name,
        group_store,
        log_level,
        log_message
    FROM
        dbo.event_log
    WHERE
        (@document_id IS NULL OR document_id = @document_id)
    AND
        (@group_id IS NULL OR group_id = @group_id)
    AND
        (@log_level IS NULL OR log_level = @log_level)
    AND
        (@start IS NULL OR log_time >= @start)
    AND
        (@end IS NULL OR log_time <= @end)
    AND
        (@message_contains IS NULL OR CHARINDEX(@message_contains, log_message) > 0)
    AND
        (@group_display_name_contains IS NULL OR CHARINDEX(@group_display_name_contains, group_display_name) > 0)
    ORDER BY
        log_time DESC;

GO
ALTER TABLE [dbo].[document] ADD  DEFAULT ((0)) FOR [published]
GO
ALTER TABLE [dbo].[document] ADD  DEFAULT ((0)) FOR [deleted]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[clone_document]
    @author nvarchar(50),
    @document_id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @json AS nvarchar(MAX);
    DECLARE @new_id uniqueidentifier;

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    SELECT
        @json = document_json
    FROM
        dbo.latest_revision
    WHERE
        document_id = @document_id

    IF @json IS NULL
        THROW 50000, 'The supplied document ID does not match an existing document.', 1
    
    SET @new_id = NEWID();
    SET @json = JSON_MODIFY(@json, '$.id', LOWER(CAST(@new_id AS nvarchar(36))));

    BEGIN TRANSACTION;

    EXECUTE dbo.new_document @author = @author, @json = @json;

    DECLARE @tag nvarchar(50);

    DECLARE
        cur
    CURSOR
        LOCAL READ_ONLY FORWARD_ONLY
    FOR
        SELECT tag FROM dbo.document_tag WHERE document_id = @document_id;

    OPEN cur;

    FETCH NEXT FROM cur INTO @tag;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXECUTE dbo.new_document_tag @author = @author, @document_id = @new_id, @tag = @tag;
        FETCH NEXT FROM cur;
    END;

    CLOSE cur;

    COMMIT TRANSACTION;

    SELECT @new_id; -- return new document ID

    RETURN;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_all_documents]
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_all_documents(@store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[get_all_documents_flattened]
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_all_documents_flattened(@store, @include_unpublished, @include_deleted);

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[get_audit_log]
    @document_id [uniqueidentifier] = NULL,
    @actor_contains [nvarchar](50) = NULL,
    @action_contains [nvarchar](100) = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
AS
BEGIN
	SET NOCOUNT ON;

	SELECT * FROM dbo.fn_get_audit_log(@document_id, @actor_contains, @action_contains, @start, @end, @count);

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_deleted_documents]
    @store nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_deleted_documents(@store);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[get_document_by_age]
    @start datetime,
    @end datetime = NULL,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_age(@start, @end, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_document_by_document_id]
    @document_id uniqueidentifier,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_document_id(@document_id, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_document_by_group_id]
    @group_id uniqueidentifier,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_group_id(@group_id, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[get_document_by_group_name]
    @group_name nvarchar(200),
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_group_name(@group_name, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[get_document_by_member_rule]
    @rule_name nvarchar(100),
    @rule_value nvarchar(200) = NULL,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_member_rule(@rule_name, @rule_value, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[get_document_by_member_source]
    @source nvarchar(50),
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_member_source(@source, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[get_document_by_processing_interval]
    @min int,
    @max int = 2147483647,
    @store nvarchar(50) = NULL,
    @include_unpublished bit = 0,
    @include_deleted bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_by_processing_interval(@min, @max, @store, @include_unpublished, @include_deleted);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_document_tags]
    @document_id uniqueidentifier,
    @single_row bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_document_tags(@document_id, @single_row);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_event_log]
    @document_id [uniqueidentifier] = NULL,
    @group_id [uniqueidentifier] = NULL,
    @message_contains nvarchar(200) = NULL,
    @group_display_name_contains nvarchar(200) = NULL,
    @log_level [int] = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
AS
BEGIN
	SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_event_log(@document_id, @group_id, @message_contains, @group_display_name_contains, @log_level, @start, @end, @count);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_operational_log]
    @document_id [uniqueidentifier] = NULL,
    @group_id [uniqueidentifier] = NULL,
    @target_id [uniqueidentifier] = NULL,
    @operation [nvarchar](20) = NULL,
    @target_display_name_contains [nvarchar](200) = NULL,
    @group_display_name_contains [nvarchar](200) = NULL,
    @start [datetime] = NULL,
    @end [datetime] = NULL,
    @count [int] = 2147483647
AS
BEGIN
	SET NOCOUNT ON;

    IF @operation NOT IN (N'Add', N'Remove')
        THROW 50000, 'Operation must be either "Add" or "Remove".', 1

    SELECT * FROM dbo.fn_get_operational_log(@document_id, @group_id, @target_id, @operation, @target_display_name_contains, @group_display_name_contains, @start, @end, @count);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[get_unpublished_documents]
    @store nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.fn_get_unpublished_documents(@store);

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[new_audit_log_entry]
    @actor nvarchar(50),
    @document_id uniqueidentifier,
    @action nvarchar(50),
    @additional_information nvarchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.audit_log
        (log_time,   document_id,  actor, [action],  additional_information)
    VALUES
        (GETDATE(), @document_id, @actor, @action, @additional_information);

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[new_document]
    @author nvarchar(50),
    @json nvarchar(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @document_id uniqueidentifier;
    DECLARE @group_id nvarchar(100);
    DECLARE @group_store nvarchar(50);
    DECLARE @delete_status bit;

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    SELECT @document_id = JSON_VALUE(@json, '$.id');
    SELECT @group_id = JSON_VALUE(@json, '$.groupId');
    SELECT @group_store = JSON_VALUE(@json, '$.store');

    SELECT @delete_status = deleted FROM dbo.latest_revision WHERE document_id = @document_id;

    IF @delete_status = 1
        THROW 50000, 'A deleted document with the same ID already exists. Restore existing document or assign a new ID.', 1;

    IF @delete_status = 0
        THROW 50000, 'A document with the same ID already exists. Update existing document or assign a new ID.', 1;

    IF EXISTS (SELECT 1 FROM dbo.latest_revision WHERE deleted = 0 AND group_id = @group_id AND group_store = @group_store)
        THROW 50000, 'A document already exists for the same group. Update existing document instead.', 1;

    BEGIN TRANSACTION;

        INSERT INTO dbo.document
           (document_id , revision, created  , published, deleted, document_json)
        VALUES
           (@document_id, 1       , GETDATE(), 0        , 0      , @json);

        EXECUTE dbo.new_audit_log_entry @author, @document_id, N'Inserted new document'

    COMMIT TRANSACTION;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[new_document_tag]
    @author nvarchar(50),
    @document_id uniqueidentifier,
    @tag nvarchar(50),
    @no_create bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @addidtional_information nvarchar(200);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    IF CHARINDEX(',', @tag) > 0
        THROW 50000, 'Tag name can not contain comma (,).', 1;

    IF EXISTS (SELECT 1 FROM dbo.document_tag WHERE document_id = @document_id AND tag = @tag)
        RETURN --Tag already exists on the document.

    IF @no_create = 1 AND NOT EXISTS (SELECT 1 FROM dbo.document_tag WHERE tag = @tag)
        THROW 50000, 'No existing tag with the same name exists and no_create was requested. Add an existing tag or remove no_create.', 1

    SET @addidtional_information = N'Tag "'+ @tag +  N'"';

    BEGIN TRANSACTION;
        INSERT INTO dbo.document_tag (document_id, tag) VALUES (@document_id, @tag);

        EXECUTE dbo.new_audit_log_entry
            @author,
            @document_id,
            N'Added new document tag',
            @addidtional_information

    COMMIT TRANSACTION;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[new_event_log_entry]
    @log_time datetime,
    @document_id nvarchar(200) = NULL,
    @group_id uniqueidentifier = NULL,
    @group_display_name nvarchar(200) = NULL,
    @group_store nvarchar(50) = NULL,
    @level tinyint,
    @message nvarchar(2000)
AS
BEGIN
	SET NOCOUNT ON;

    INSERT INTO dbo.event_log
        ( log_time,  document_id,  group_id,  group_display_name,  group_store,  log_level,  log_message)
    VALUES
        (@log_time, @document_id, @group_id, @group_display_name, @group_store, @level, @message);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[new_operational_log_entry]
    @log_time datetime,
    @document_id uniqueidentifier,
    @group_id uniqueidentifier,
    @group_display_name nvarchar(200),
    @group_store nvarchar(50),
    @target_id uniqueidentifier,
    @target_display_name nvarchar(200),
    @operation nvarchar(10)
AS
BEGIN
	SET NOCOUNT ON;

    INSERT INTO dbo.operational_log
        ( log_time,  document_id,  group_id,  group_display_name,  group_store,  target_id,  target_display_name,  operation)
    VALUES
        (@log_time, @document_id, @group_id, @group_display_name, @group_store, @target_id, @target_display_name, @operation);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[remove_document_tag]
    @author nvarchar(50),
    @document_id uniqueidentifier,
    @tag nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @addidtional_information nvarchar(200);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    -- If there is no tag we just return. Trying to remove a non existing tag does not risk
    -- data integrity. This check is only here to avoid adding a non event to the audit log.
    IF NOT EXISTS (SELECT 1 FROM dbo.document_tag WHERE document_id = @document_id AND tag = @tag)
        RETURN

    SET @addidtional_information = N'Tag "'+ @tag +  N'"';

    BEGIN TRANSACTION;
        DELETE FROM dbo.document_tag WHERE document_id = @document_id AND tag = @tag;

        EXECUTE dbo.new_audit_log_entry
            @author,
            @document_id,
            N'Removed document tag',
            @addidtional_information

    COMMIT TRANSACTION;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[revert_to_revision]
    @author nvarchar(50),
    @document_id uniqueidentifier,
    @revision int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @current_revision int;
    DECLARE @operation nvarchar(100);
    DECLARE @additional_information nvarchar(200);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    SELECT @current_revision = MAX(revision) FROM dbo.document WHERE document_id = @document_id;

    IF @current_revision IS NULL
        THROW 50000, 'Could not find a revision for document.', 1;

    IF EXISTS (SELECT 1 FROM dbo.document WHERE document_id = @document_id AND revision = @current_revision AND deleted = 1)
        THROW 50000, 'Document is deleted. Recover deleted document first.', 1;

    -- If no specific revision is requested, revert to the revision immediately before current.
    IF @revision IS NULL
        SELECT @revision = revision FROM dbo.document ORDER BY revision DESC OFFSET 1 ROW FETCH NEXT 1 ROW ONLY;

    IF @revision = @current_revision
        THROW 50000, 'The revision you are trying to restore is already the current revision.', 1;

    SET @operation = N'Revert to revision ' + CAST(@revision AS nvarchar)
    SET @additional_information = N'New revision ' + CAST(@current_revision + 1 AS nvarchar)

    BEGIN TRANSACTION;

        INSERT INTO dbo.document
            (document_id, revision, created, published, deleted, document_json)
        SELECT
            document_id,
            @current_revision + 1, -- PK on document table will prevent duplicate revision ids
            GETDATE(),
            0, -- published
            0, -- deleted
            document_json
        FROM
            dbo.document
        WHERE
            document_id = @document_id
        AND
            revision = @revision;

        EXECUTE new_audit_log_entry @author,  @document_id, @operation, @additional_information;

    COMMIT TRANSACTION;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


/* Used by SSRS reports to search the operational log */
CREATE PROCEDURE [dbo].[search_operational_log]
    @search nvarchar(200) = NULL
AS
BEGIN
	SET NOCOUNT ON;

    SELECT * FROM dbo.fn_search_operational_log(@search);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[set_deleted]
    @author nvarchar(50),
    @document_id uniqueidentifier,
    @deleted bit
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @current_delete_status bit;
    DECLARE @group_id nvarchar(100);
    DECLARE @group_store nvarchar(50);
    DECLARE @operation nvarchar(100);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    SELECT
        @current_delete_status = deleted,
        @group_id = group_id,
        @group_store = group_store
    FROM
        dbo.latest_revision
    WHERE
        document_id = @document_id; 

    IF @current_delete_status = @deleted
        RETURN -- no change
       
    SELECT @operation = CASE WHEN @deleted = 0 THEN N'Restored deleted document' ELSE N'Deleted document' END;

    BEGIN TRANSACTION;

        -- Update deleted flag on all revisions. This is an extra safety measure in case 
        -- the document fetched is not the latest revision.
        UPDATE
            dbo.document
        SET
            deleted = @deleted,
            published = 0
        WHERE
            document_id = @document_id

        EXECUTE dbo.new_audit_log_entry @author, @document_id, @operation;

    COMMIT TRANSACTION;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[set_published]
    @author nvarchar(50),
    @document_id uniqueidentifier,
    @published bit
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @current_publish_status bit;
    DECLARE @current_delete_status bit;
    DECLARE @operation nvarchar(100);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    IF @published = 1 AND EXISTS (SELECT * FROM dbo.document a INNER JOIN dbo.document b ON a.group_id = b.group_id AND a.group_store = b.group_store WHERE a.document_id = @document_id AND b.published = 1 AND b.document_id <> @document_id)
        THROW 50000, 'A published document already exists for the same group. Unpublish existing document before proceeding.', 1

    SELECT
        @current_publish_status = published,
        @current_delete_status = deleted
    FROM
        dbo.latest_revision
    WHERE
        document_id = @document_id; 

    IF @current_publish_status = @published
        RETURN -- no change

    IF @current_delete_status = 1
        THROW 50000, 'Cannot update a deleted document.', 1;

    SELECT @operation = CASE WHEN @published = 0 THEN N'Unpublished document' ELSE N'Published document' END;

    BEGIN TRANSACTION;

        UPDATE
            dbo.document
        SET
            published = @published
        WHERE
            document_id = @document_id
        AND
            revision = (SELECT MAX(revision) FROM dbo.document WHERE document_id = @document_id);

        EXECUTE dbo.new_audit_log_entry @author, @document_id, @operation;
    
    COMMIT TRANSACTION;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[store_document]
    @author nvarchar(50),
    @json nvarchar(MAX)
AS
BEGIN
    -- store_document combines new_document and update_document in one operation

    SET NOCOUNT ON;

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    DECLARE @document_id uniqueidentifier;
    SELECT @document_id = JSON_VALUE(@json, '$.id');

    IF EXISTS (SELECT 1 FROM dbo.latest_revision WHERE document_id = @document_id)
        EXECUTE dbo.update_document @author = @author, @json = @json;
    ELSE
        EXECUTE dbo.new_document @author = @author, @json = @json;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[update_document]
    @author nvarchar(50),
    @json nvarchar(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @document_id uniqueidentifier;
    DECLARE @group_id nvarchar(100);
    DECLARE @group_store nvarchar(50);

    IF @author IS NULL OR LEN(@author) = 0
        THROW 50000, '@author cannot be null or empty.', 1;

    SELECT @document_id = JSON_VALUE(@json, '$.id');
    SELECT @group_id = JSON_VALUE(@json, '$.groupId');
    SELECT @group_store = JSON_VALUE(@json, '$.store');

    IF EXISTS (SELECT 1 FROM dbo.latest_revision WHERE deleted = 1 AND document_id = @document_id)
        THROW 50000, 'Document is deleted. Restore document before updating.', 1
    
    IF EXISTS (SELECT 1 FROM dbo.latest_revision WHERE deleted = 0 AND group_id = @group_id AND group_store = @group_store AND document_id <> @document_id)
        THROW 50000, 'A document already exists for the same group. Update existing document instead.', 1;

    IF EXISTS (SELECT 1 FROM dbo.latest_revision WHERE deleted = 0 AND document_id = @document_id AND (group_id <> @group_id OR group_store <> @group_store))
        THROW 50000, 'Group ID and store cannot be changed. Create a new document instead.', 1;

    BEGIN TRANSACTION;
        UPDATE dbo.document SET published = 0 WHERE document_id = @document_id;

        INSERT INTO dbo.document
            (document_id, revision, created, published, deleted, document_json)
        VALUES (
            @document_id,
            (SELECT MAX(revision) + 1 FROM dbo.document WHERE document_id = @document_id),
            GETDATE(),
            0, -- published
            0, -- deleted
            @json
        );

        EXECUTE dbo.new_audit_log_entry @author, @document_id, N'Updated document'

    COMMIT TRANSACTION;

END
GO
