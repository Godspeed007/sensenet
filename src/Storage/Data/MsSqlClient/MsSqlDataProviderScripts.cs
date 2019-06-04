﻿using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    public partial class MsSqlDataProvider
    {
        /* ------------------------------------------------ Nodes */

        #region InsertNodeAndVersionScript
        public override string InsertNodeAndVersionScript => @"-- MsSqlDataProvider.InsertNodeAndVersion
DECLARE @NodeId int, @VersionId int
DECLARE @NodeTimestamp timestamp, @VersionTimestamp timestamp

INSERT INTO Nodes
    ([NodeTypeId],[ContentListTypeId],[ContentListId],[CreatingInProgress],[IsDeleted],[IsInherited],[ParentNodeId],[Name],[DisplayName],[Index],[Locked],[LockedById],[ETag],[LockType],[LockTimeout],[LockDate],[LockToken],[LastLockUpdate],    [CreationDate],    [CreatedById],    [ModificationDate],    [ModifiedById],[IsSystem],[OwnerId],[SavingState],[Path]) VALUES
    (@NodeTypeId, @ContentListTypeId, @ContentListId, @CreatingInProgress, @IsDeleted, @IsInherited, @ParentNodeId, @Name, @DisplayName, @Index, @Locked, @LockedById, @ETag, @LockType, @LockTimeout, @LockDate, @LockToken, @LastLockUpdate, @NodeCreationDate, @NodeCreatedById, @NodeModificationDate, @NodeModifiedById, @IsSystem, @OwnerId, @SavingState, @Path )

SELECT @NodeId = @@IDENTITY

-- skip the rest, if the insert above was not successful
IF (@NodeId is NOT NULL)
BEGIN
    INSERT INTO Versions 
        ([NodeId],[MajorNumber],[MinorNumber],       [CreationDate],       [CreatedById],       [ModificationDate],       [ModifiedById],[Status],[ChangedData],[DynamicProperties]) VALUES
        (@NodeId, @MajorNumber, @MinorNumber, @VersionCreationDate, @VersionCreatedById, @VersionModificationDate, @VersionModifiedById, @Status, @ChangedData, @DynamicProperties )

    SELECT @VersionId = @@IDENTITY
    SELECT @VersionTimestamp = [Timestamp] FROM Versions WHERE VersionId = @VersionId

    IF @Status = 1
        UPDATE Nodes SET LastMinorVersionId = @VersionId, LastMajorVersionId = @VersionId WHERE NodeId = @NodeId
    ELSE
        UPDATE Nodes SET LastMinorVersionId = @VersionId WHERE NodeId = @NodeId
    SELECT @NodeTimestamp = [Timestamp] FROM Nodes WHERE NodeId = @NodeId

    SELECT @NodeId NodeId, @VersionId VersionId, @NodeTimestamp NodeTimestamp, @VersionTimestamp VersionTimestamp, LastMajorVersionId, LastMinorVersionId, Path FROM Nodes WHERE NodeId = @NodeId
END
";
        #endregion
        #region InsertLongtextPropertiesHeadScript
        public override string InsertLongtextPropertiesHeadScript => @"-- MsSqlDataProvider.InsertLongtextProperties
";
        #endregion
        #region InsertLongtextPropertiesScript
        public override string InsertLongtextPropertiesScript => @"INSERT INTO LongTextProperties
    ([VersionId],[PropertyTypeId],[Length],[Value]) VALUES
    (@VersionId, @PropertyTypeId{0}, @Length{0}, @Value{0} )
";
        #endregion

        #region UpdateVersionScript
        public override string UpdateVersionScript => @"-- MsSqlDataProvider.UpdateVersion
UPDATE Versions SET
	NodeId = @NodeId,
	MajorNumber = @MajorNumber,
	MinorNumber = @MinorNumber,
	CreationDate = @CreationDate,
	CreatedById = @CreatedById,
	ModificationDate = @ModificationDate,
	ModifiedById = @ModifiedById,
	Status = @Status,
	ChangedData = @ChangedData,
    DynamicProperties = @DynamicProperties
WHERE VersionId = @VersionId

SELECT [Timestamp] FROM Versions WHERE VersionId = @VersionId
";
        #endregion
        #region UpdateNodeScript
        public override string UpdateNodeScript => @"-- MsSqlDataProvider.UpdateNode
UPDATE Nodes SET
	NodeTypeId = @NodeTypeId,
	ContentListTypeId = @ContentListTypeId,
	ContentListId = @ContentListId,
	CreatingInProgress = @CreatingInProgress,
	IsDeleted = @IsDeleted,
	IsInherited = @IsInherited,
	ParentNodeId = @ParentNodeId,
	[Name] = @Name,
	DisplayName = @DisplayName,
	Path = @Path,
	[Index] = @Index,
	Locked = @Locked,
	LockedById = @LockedById,
	ETag = @ETag,
	LockType = @LockType,
	LockTimeout = @LockTimeout,
	LockDate = @LockDate,
	LockToken = @LockToken,
	LastLockUpdate = @LastLockUpdate,
	CreationDate = @CreationDate,
	CreatedById = @CreatedById,
	ModificationDate = @ModificationDate,
	ModifiedById = @ModifiedById,
	IsSystem = @IsSystem,
	OwnerId = @OwnerId,
	SavingState = @SavingState
FROM
	Nodes
WHERE NodeId = @NodeId AND [Timestamp] = @NodeTimestamp

IF @@ROWCOUNT = 0 BEGIN
	DECLARE @Count int
	SELECT @Count = COUNT(*) FROM Nodes WHERE NodeId = @NodeId
	IF @Count = 0
		RAISERROR (N'Cannot update a deleted Node. Id: %d, path: %s.', 12, 1, @NodeId, @Path);
	ELSE
		RAISERROR (N'Node is out of date Id: %d, path: %s.', 12, 1, @NodeId, @Path);
END
ELSE BEGIN
	SELECT [Timestamp] FROM Nodes WHERE NodeId = @NodeId
END
";
        #endregion
        #region UpdateSubTreePathScript
        public override string UpdateSubTreePathScript => @"-- MsSqlDataProvider.UpdateSubTreePath
DECLARE @OldPathLen int
SET @OldPathLen = LEN(@OldPath)

UPDATE Nodes
SET Path = @NewPath + RIGHT(Path, LEN(Path) - @OldPathLen)
WHERE Path LIKE REPLACE(@OldPath, '_', '[_]') + '/%'
";
        #endregion
        #region UpdateLongtextPropertiesHeadScript
        public override string UpdateLongtextPropertiesHeadScript => @"-- MsSqlDataProvider.UpdateLongtextProperties
";
        #endregion
        #region UpdateLongtextPropertiesScript
        public override string UpdateLongtextPropertiesScript => @"-- MsSqlDataProvider.UpdateLongtextProperties
DELETE FROM LongTextProperties WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId{0}
" + InsertLongtextPropertiesScript;
        #endregion

        #region LoadTextPropertyValuesScript
        public override string LoadTextPropertyValuesScript => @"-- MsSqlDataProvider.LoadTextPropertyValues
SELECT PropertyTypeId, Value FROM LongTextProperties WHERE VersionId = @VersionId AND PropertyTypeId IN ({0})
";
        #endregion

        #region LoadNodesScript
        public override string LoadNodesScript => @"-- MsSqlDataProvider.LoadNodes
-- Transform the input to a queryable format
DECLARE @VersionIdTable AS TABLE(Id INT)
INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');

-- BaseData
SELECT N.NodeId, N.NodeTypeId, N.ContentListTypeId, N.ContentListId, N.CreatingInProgress, N.IsDeleted, N.IsInherited, 
    N.ParentNodeId, N.[Name], N.DisplayName, N.[Path], N.[Index], N.Locked, N.LockedById, 
    N.ETag, N.LockType, N.LockTimeout, N.LockDate, N.LockToken, N.LastLockUpdate,
    N.CreationDate AS NodeCreationDate, N.CreatedById AS NodeCreatedById, 
    N.ModificationDate AS NodeModificationDate, N.ModifiedById AS NodeModifiedById,
    N.IsSystem, N.OwnerId,
    N.SavingState, V.ChangedData,
    N.Timestamp AS NodeTimestamp,
    V.VersionId, V.MajorNumber, V.MinorNumber, V.CreationDate, V.CreatedById, 
    V.ModificationDate, V.ModifiedById, V.[Status],
    V.Timestamp AS VersionTimestamp,
    V.DynamicProperties
FROM dbo.Nodes AS N 
    INNER JOIN dbo.Versions AS V ON N.NodeId = V.NodeId
WHERE V.VersionId IN (select Id from @VersionIdTable)

-- BinaryProperties
SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
    F.Extension, F.[Size], F.[BlobProvider], F.[BlobProviderData], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp]
FROM dbo.BinaryProperties B
    JOIN dbo.Files F ON B.FileId = F.FileId
WHERE VersionId IN (select Id from @VersionIdTable) AND Staging IS NULL

    -- ReferenceProperties
    --SELECT VersionId, PropertyTypeId, ReferredNodeId
    --FROM dbo.ReferenceProperties
    --WHERE VersionId IN (select Id from @VersionIdTable)

-- LongTextProperties
SELECT VersionId, PropertyTypeId, [Length], [Value]
FROM dbo.LongTextProperties
WHERE VersionId IN (SELECT Id FROM @VersionIdTable) AND Length < @LongTextMaxSize
";
        #endregion

        #region DeleteVersionsScript
        public override string DeleteVersionsScript => @"-- MsSqlDataProvider.DeleteVersions
DECLARE @VersionIdTable AS TABLE(Id INT)
INSERT INTO @VersionIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@VersionIds, ',');

DELETE FROM LongTextProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)
--DELETE FROM ReferenceProperties WHERE VersionId IN (SELECT Id FROM @VersionIdTable)

UPDATE Nodes SET LastMinorVersionId = NULL, LastMajorVersionId = NULL WHERE NodeId = @NodeId
DELETE FROM Versions WHERE VersionId IN (SELECT Id FROM @VersionIdTable)

UPDATE Nodes
	SET LastMinorVersionId = (SELECT TOP (1) VersionId FROM Versions WHERE NodeId = @NodeId
			ORDER BY MajorNumber DESC, MinorNumber DESC),
		LastMajorVersionId = (SELECT TOP (1) VersionId FROM Versions WHERE NodeId = @NodeId AND MinorNumber = 0 AND Status = 1
			ORDER BY MajorNumber DESC, MinorNumber DESC)
WHERE NodeId = @NodeId

-- Return the new timestamp and version ids
SELECT [Timestamp] as NodeTimestamp, LastMajorVersionId, LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId
";
        #endregion


        /* ------------------------------------------------ NodeHead */

        #region LoadNodeHeadScript
        private string LoadNodeHeadSkeletonScript = @"-- MsSqlDataProvider.{0}
{1}
SELECT
    Node.NodeId,             -- 0
    Node.Name,               -- 1
    Node.DisplayName,        -- 2
    Node.Path,               -- 3
    Node.ParentNodeId,       -- 4
    Node.NodeTypeId,         -- 5
    Node.ContentListTypeId,  -- 6
    Node.ContentListId,      -- 7
    Node.CreationDate,       -- 8
    Node.ModificationDate,   -- 9
    Node.LastMinorVersionId, -- 10
    Node.LastMajorVersionId, -- 11
    Node.OwnerId,            -- 12
    Node.CreatedById,        -- 13
    Node.ModifiedById,       -- 14
    Node.[Index],            -- 15
    Node.LockedById,         -- 16
    Node.Timestamp           -- 17
FROM
    Nodes Node
    {2}
WHERE 
    {3}
";

        private string LoadNodeHeadScript(string trace, string scriptHead = null, string join = null, string where = null)
        {
            return string.Format(LoadNodeHeadSkeletonScript,
                trace,
                scriptHead ?? string.Empty,
                join ?? string.Empty,
                where ?? string.Empty);
        }

        public override string LoadNodeHeadByPathScript => LoadNodeHeadScript("LoadNodeHead by Path", where: "Node.Path = @Path COLLATE Latin1_General_CI_AS");
        public override string LoadNodeHeadByIdScript => LoadNodeHeadScript("LoadNodeHead by NodeId", where: "Node.NodeId = @NodeId");
        public override string LoadNodeHeadByVersionIdScript => LoadNodeHeadScript("LoadNodeHead by VersionId",
            join: "JOIN Versions V ON V.NodeId = Node.NodeId", where: "V.VersionId = @VersionId");
        public override string LoadNodeHeadsByIdSetScript => LoadNodeHeadScript("LoadNodeHead by NodeId set",
            scriptHead: @"DECLARE @NodeIdTable AS TABLE(Id INT) INSERT INTO @NodeIdTable SELECT CONVERT(int, [value]) FROM STRING_SPLIT(@NodeIds, ',');",
            where: "Node.NodeId IN (SELECT Id FROM @NodeIdTable)");
        #endregion

        /* ------------------------------------------------ NodeQuery */

        /* ------------------------------------------------ Tree */

        /* ------------------------------------------------ TreeLock */

        #region IsTreeLockedScript
        public override string IsTreeLockedScript => @"-- MsSqlDataProvider.IsTreeLocked
SELECT TreeLockId
FROM TreeLocks
WHERE @TimeLimit < LockedAt AND (
    [Path] LIKE (REPLACE(@Path0, '_', '[_]') + '/%') OR
    [Path] IN ( {0} ) )
";
        #endregion

        /* ------------------------------------------------ IndexDocument */

        #region SaveIndexDocumentScript
        public override string SaveIndexDocumentScript => @"-- MsSqlDataProvider.SaveIndexDocument
UPDATE Versions SET [IndexDocument] = @IndexDocument WHERE VersionId = @VersionId
SELECT Timestamp FROM Versions WHERE VersionId = @VersionId"
;
        #endregion

        /* ------------------------------------------------ IndexingActivity */

        #region GetLastIndexingActivityIdScript
        public override string GetLastIndexingActivityIdScript => @"-- MsSqlDataProvider.GetLastIndexingActivityId
SELECT CASE WHEN i.last_value IS NULL THEN 0 ELSE CONVERT(int, i.last_value) END last_value FROM sys.identity_columns i JOIN sys.tables t ON i.object_id = t.object_id WHERE t.name = 'IndexingActivities'";
        #endregion

        /* ------------------------------------------------ Schema */

        #region LoadSchemaScript
        //UNDONE:DB: LoadSchema script: ContentListTypes is commnted out
        public override string LoadSchemaScript => @"-- MsSqlDataProvider.LoadSchema
SELECT [Timestamp] FROM SchemaModification
SELECT * FROM PropertyTypes
SELECT * FROM NodeTypes
--SELECT * FROM ContentListTypes
";
        #endregion

        /* ------------------------------------------------ Logging */

        #region WriteAuditEventScript
        public override string WriteAuditEventScript => @"-- MsSqlDataProvider.WriteAuditEvent
INSERT INTO [dbo].[LogEntries]
    ([EventId], [Category], [Priority], [Severity], [Title], [ContentId], [ContentPath], [UserName], [LogDate], [MachineName], [AppDomainName], [ProcessId], [ProcessName], [ThreadName], [Win32ThreadId], [Message], [FormattedMessage])
VALUES
    (@EventId,  @Category,  @Priority,  @Severity,  @Title,  @ContentId,  @ContentPath,  @UserName,  @LogDate,  @MachineName,  @AppDomainName,  @ProcessId,  @ProcessName,  @ThreadName,  @Win32ThreadId,  @Message,  @FormattedMessage)
SELECT @@IDENTITY
";
        #endregion

        /* ------------------------------------------------ Provider Tools */

        #region GetTreeSizeScript
        public override string GetTreeSizeScript => @"-- MsSqlDataProvider.GetTreeSize
SELECT SUM(F.Size) Size
FROM Files F
    JOIN BinaryProperties B ON B.FileId = F.FileId
    JOIN Versions V on V.VersionId = B.VersionId
    JOIN Nodes N on V.NodeId = N.NodeId
WHERE F.Staging IS NULL AND (N.[Path] = @NodePath OR (@IncludeChildren = 1 AND N.[Path] + '/' LIKE REPLACE(@NodePath, '_', '[_]') + '/%'))
";
        #endregion

        /* ------------------------------------------------ Installation */

        #region LoadEntityTreeScript
        public override string LoadEntityTreeScript { get; } = @"-- MsSqlDataProvider.LoadEntityTree
SELECT NodeId, ParentNodeId, OwnerId FROM Nodes ORDER BY Path
";
        #endregion

        /* ------------------------------------------------ Tools */
    }

}
