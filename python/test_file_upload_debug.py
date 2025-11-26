#!/usr/bin/env python3
"""
Debug test to see GraphQL responses
"""

import asyncio
import eywa
import uuid
import json


async def test_request_upload_url():
    """Test the requestUploadURL mutation directly"""
    eywa.open_pipe()

    try:
        # Test 1: Request upload URL directly
        eywa.info("🔍 Testing requestUploadURL mutation...")

        file_input = {
            "euuid": str(uuid.uuid4()),
            "name": "test-debug.txt",
            "content_type": "text/plain",
            "size": 100,
        }

        eywa.info(f"📤 Sending GraphQL mutation with input: {json.dumps(file_input, indent=2)}")

        upload_query = """
        mutation RequestUpload($file: FileInput!) {
            requestUploadURL(file: $file)
        }
        """

        result = await eywa.graphql(upload_query, {"file": file_input})

        eywa.info(f"📥 Full GraphQL response: {json.dumps(result, indent=2)}")

        upload_url = result.get("requestUploadURL")
        if upload_url:
            eywa.info(f"✅ Got upload URL: {upload_url[:100]}...")
        else:
            eywa.error(f"❌ No upload URL in response. Response keys: {list(result.keys())}")

        # Test 2: Try to create a folder
        eywa.info("\n🔍 Testing folder creation mutation...")

        folder_input = {
            "euuid": str(uuid.uuid4()),
            "name": "test-debug-folder",
            "parent": {"euuid": "87ce50d8-5dfa-4008-a265-053e727ab793"}  # ROOT_UUID
        }

        eywa.info(f"📤 Sending folder creation with input: {json.dumps(folder_input, indent=2)}")

        folder_query = """
        mutation CreateFolder($data: FolderInput!) {
            createFolder(data: $data) {
                euuid
                name
                path
            }
        }
        """

        folder_result = await eywa.graphql(folder_query, {"data": folder_input})

        eywa.info(f"📥 Folder creation response: {json.dumps(folder_result, indent=2)}")

        # Cleanup - delete the folder
        if folder_result.get("createFolder"):
            folder_uuid = folder_result["createFolder"]["euuid"]
            delete_query = """
            mutation DeleteFolder($uuid: UUID!) {
                deleteFolder(euuid: $uuid)
            }
            """
            await eywa.graphql(delete_query, {"uuid": folder_uuid})
            eywa.info(f"🧹 Cleaned up test folder")

        eywa.close_task(eywa.SUCCESS)

    except Exception as e:
        eywa.error(f"💥 Error: {e}")
        import traceback
        eywa.error(traceback.format_exc())
        eywa.close_task(eywa.ERROR)


if __name__ == "__main__":
    asyncio.run(test_request_upload_url())
