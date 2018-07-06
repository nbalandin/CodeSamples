/*
IF EXISTS (
		SELECT *
		FROM sys.objects
		WHERE object_id = OBJECT_ID(N'www.proc_Devices_MEP_Count')
			AND type IN (
				N'P'
				,N'PC'
				)
		)
	DROP PROCEDURE [www].[proc_Devices_MEP_Count]
GO
*/
--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
---- =============================================
---- Author:		A.Sribnyak
---- Create date: 20150318
---- Description:	List devices MEP
---- =============================================
/*
Read On Demand Access -> COSPrivileges_bit0
Shedule Read Access -> COSPrivileges_bit2
Send M-BUS/MEP Command Access -> COSPrivileges_bit1
*/
CREATE PROCEDURE www.proc_Devices_MEP_Count 
(
	@filter NVARCHAR(2000) = ''
	,@ParentDeviceID NVARCHAR(32) = NULL
	,@HierarchyID NVARCHAR(32) = NULL
	,@attributAttributes [www].[Attributes] readonly 
	,@assigned BIT=1
	,@ParentHierarchyID NVARCHAR(32) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;


	DECLARE @sql AS NVARCHAR(MAX) = ''
	DECLARE @cnt AS INT

	--Prepare Status Filter
	SELECT @filter=REPLACE (@filter, 'Status', 'StatusTypeID')
	SELECT @filter=REPLACE (@filter, 'TimeZone', 'TimezoneID')
	SELECT @filter=REPLACE (@filter, 'Description', 'DeviceDescription')
	SELECT @filter=REPLACE (@filter, 'ParentDeviceID', 'Devices1.ParentDeviceID')

	--Prepare attributes
	declare @cntAttr int
	declare @strAttr nvarchar(2000)=''
	select @cntAttr=COUNT(*)  from @attributAttributes
	if @cntAttr > 0 
	BEGIN
		exec www.proc_BuildAttributesFilter @attributAttributes,@Type='device',  @StrOut=@strAttr OUTPUT
		--SELECT @strAttr
	END

	--Prepare Hierarchy
	IF @HierarchyID IS NOT NULL
	BEGIN
		CREATE TABLE #Table_Hierarchy (HierarchyLevelMemberID NVARCHAR(32))
		IF @assigned=1
			BEGIN
				INSERT INTO #Table_Hierarchy
				select HierarchyLevelMemberID 
				from [www].[fn_Hierarchy](@HierarchyID)
				CREATE CLUSTERED INDEX IX_T_Table_Hierarchy  on #Table_Hierarchy (HierarchyLevelMemberID) 	
			END
	END
	
	SET @sql = '/*' + @filter + isnull(@HierarchyID,'')+'*/ '

	SET @sql = @sql + '

		SELECT
			 @cntOUT=count(*)
		
		FROM [www].[vDevices_MEP] d with (nolock, NOEXPAND) ' 
		+ CASE WHEN @HierarchyID IS NOT NULL THEN
			CASE WHEN @assigned = 1 then 
			'	INNER JOIN DeviceHierarchies DH ON D.DeviceID = DH.DeviceID
				INNER JOIN #Table_Hierarchy h on DH.HierarchyLevelMemberID=h.HierarchyLevelMemberID '
			ELSE 
				' LEFT OUTER JOIN DeviceHierarchies DH ON D.DeviceID = DH.DeviceID  AND DH.SysSwHierarchyID= N'''  + @ParentHierarchyID + ''''
			END
		ELSE
			' '
		END
		+
 		CASE 
		WHEN LEN(@ParentDeviceID) > 0
			THEN ' INNER JOIN (SELECT DeviceID, ParentDeviceID from  Devices nolock) as Devices on d.DeviceId=Devices.DeviceId and Devices.ParentDeviceID=' + ''''+ @ParentDeviceID + ''''+' '
		ELSE ' '
		END 
		+
 		CASE 
		WHEN PATINDEX('%ParentDeviceId%', @filter) > 0
			THEN ' INNER JOIN (SELECT DeviceID, ParentDeviceID from  Devices nolock) as Devices1 on d.DeviceId=Devices1.DeviceId '
		ELSE ' '
		END 
		+
		CASE WHEN LEN(@strAttr)>0
		THEN
			+' INNER JOIN ('+@strAttr+') a on d.DeviceId=a.DeviceId'
		ELSE
			''
		END
		+
		CASE 
		WHEN PATINDEX('%COSPrivileges%', @filter) > 0
			THEN ' INNER JOIN Devices_Privileges DP ON d.DeviceId=DP.DeviceId '
		ELSE ''	
		END 
		+
		CASE 
			WHEN LEN(@FILTER) > 0 OR (@HierarchyID IS NOT NULL AND @assigned = 0)
				THEN 'WHERE ' + isnull(@filter , '')
			+
			CASE WHEN @HierarchyID IS NOT NULL AND @assigned = 0 THEN 
			CASE WHEN LEN(@filter)<>0 then  ' AND ' ELSE ' ' END +' DH.DeviceID is null ' ELSE '' END
			ELSE ' ' END

	--select @sql
	
	EXEC sp_executesql @sql
		,N'	@cntOUT int OUTPUT'
		,@cntOUT = @cnt OUTPUT

		SELECT @cnt

END
--exec [www].[proc_Devices_MEP_Count]
--exec [www].[proc_Devices_MEP_Count] @filter = 'Name like ''%23%'' '
--exec [www].[proc_Devices_MEP_Count] @filter = 'Name not like ''%23%'' '
--exec [www].[proc_Devices_MEP_Count] @filter = 'Status like ''%5E96BEFC1BFD4c6cA42CE2F9BF86FA37%'''
--exec [www].[proc_Devices_MEP_Count] @filter = 'TimeZone = ''2356cd546ecf4bab9657879ffeac4c0d'''
--exec [www].[proc_Devices_MEP_Count] @filter = 'Description not like ''%1%'''
--exec [www].[proc_Devices_MEP_Count] @filter = 'Name not like ''%23%'' ', @HierarchyID='ab1ac04c52a54dd1b54a597ad6d4c482'
--exec [www].[proc_Devices_MEP_Count] @ParentDeviceID='DC27EE887108B5A98540DF1ABE0737FD'
--exec [www].[proc_Devices_MEP_Count] @ParentDeviceID='DC27EE887108B5A98540DF1ABE0737FD', @filter ='ParentDeviceID= ''DC27EE887108B5A98540DF1ABE0737FD'''

/*
		declare @attributAttributes [www].[Attributes]
		insert into @attributAttributes  values ('cd186dc66d884ce792ca195de0a24d0f', 1000,1,1)
		exec [www].[proc_Devices_MEP_Count] @attributAttributes=@attributAttributes
		
*/
--exec [www].[proc_Devices_MEP_Count] @filter = 'COSPrivileges_bit2 = N''1'' '
--exec [www].[proc_Devices_MEP_Count] @filter = 'COSPrivileges_bit1 = N''0'' and COSPrivileges_bit0 = N''0'''
	--exec [www].[proc_Devices_MEP_Count] @HierarchyID='a6e39a0514304ae983e27c2b521df96a'
	--exec [www].[proc_Devices_MEP_Count] @HierarchyID='a6e39a0514304ae983e27c2b521df96a', @assigned=0, @ParentHierarchyID='d41b3cf7bd184656bef26246c309368f'
	--exec [www].[proc_Devices_MEP_Count] @HierarchyID='d41b3cf7bd184656bef26246c309368f'
	--exec [www].[proc_Devices_MEP_Count] @filter = 'ManufacturerID  like ''%18'''
GO