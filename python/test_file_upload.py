#!/usr/bin/env python3
"""
EYWA File Upload Test
Tests the eywa-client library file upload/download functionality.

Usage: eywa run -c "python python/test_file_upload.py"

This test will:
- Create a test folder
- Upload text and JSON files
- Download and verify file contents
- Test error handling
- Clean up all test resources
"""

import asyncio
import eywa
from eywa_files import (
    upload,
    upload_content,
    download,
    create_folder,
    delete_file,
    delete_folder,
    ROOT_UUID,
    FileUploadError,
    FileDownloadError,
)
import uuid
import tempfile
import os
from pathlib import Path
import json


# Pre-defined UUIDs for test resources (proper UUID format)
TEST_FOLDER_UUID = str(uuid.uuid4())
TEST_TEXT_FILE_UUID = str(uuid.uuid4())
TEST_JSON_FILE_UUID = str(uuid.uuid4())


class FileUploadTest:
    def __init__(self):
        self.test_resources = []
        self.test_passed = 0
        self.test_failed = 0

    def track_resource(self, resource_type: str, resource_uuid: str, name: str):
        """Track resource for cleanup"""
        self.test_resources.append(
            {"type": resource_type, "uuid": resource_uuid, "name": name}
        )

    async def cleanup(self):
        """Clean up all test resources"""
        eywa.info("🧹 Cleaning up test resources...")

        # Delete files first
        for resource in self.test_resources:
            if resource["type"] == "file":
                try:
                    await delete_file(resource["uuid"])
                    eywa.info(f"✅ Deleted file: {resource['name']}")
                except Exception as e:
                    eywa.warn(f"Failed to delete file {resource['name']}: {e}")

        # Then delete folders
        for resource in self.test_resources:
            if resource["type"] == "folder":
                try:
                    await delete_folder(resource["uuid"])
                    eywa.info(f"✅ Deleted folder: {resource['name']}")
                except Exception as e:
                    eywa.warn(f"Failed to delete folder {resource['name']}: {e}")

    def assert_test(self, condition: bool, test_name: str, error_msg: str = ""):
        """Simple assertion helper"""
        if condition:
            self.test_passed += 1
            eywa.info(f"✅ PASS: {test_name}")
            return True
        else:
            self.test_failed += 1
            eywa.error(f"❌ FAIL: {test_name}" + (f" - {error_msg}" if error_msg else ""))
            return False

    async def test_create_folder(self):
        """Test 1: Create a test folder"""
        eywa.info("\n📁 TEST 1: Creating test folder...")

        try:
            folder_uuid = TEST_FOLDER_UUID
            self.track_resource("folder", folder_uuid, "test-upload-folder")

            folder = await create_folder(
                {
                    "euuid": folder_uuid,
                    "name": "test-upload-folder",
                    "parent": {"euuid": ROOT_UUID},
                }
            )

            eywa.info(f"🔍 DEBUG: create_folder response: {folder}")

            self.assert_test(
                folder is not None and folder.get("euuid") == folder_uuid,
                "Folder created successfully",
            )

            # Verify with GraphQL
            verification = await eywa.graphql(
                """
                query GetFolder($uuid: UUID!) {
                    getFolder(euuid: $uuid) {
                        euuid
                        name
                        path
                    }
                }
            """,
                {"uuid": folder_uuid},
            )

            eywa.info(f"🔍 DEBUG: getFolder response: {verification}")

            folder_info = verification.get("getFolder")
            self.assert_test(
                folder_info is not None,
                "Folder verified via GraphQL",
                f"Expected folder with UUID {folder_uuid}, got None",
            )

            return folder_uuid

        except Exception as e:
            eywa.error(f"🔍 DEBUG: Folder creation exception: {e}")
            self.assert_test(False, "Folder creation", str(e))
            return None

    async def test_upload_text_file(self, folder_uuid: str):
        """Test 2: Upload a text file"""
        eywa.info("\n📤 TEST 2: Uploading text file...")

        temp_file = None
        try:
            # Create temporary text file
            temp_file = tempfile.NamedTemporaryFile(
                mode="w", suffix=".txt", delete=False
            )
            test_content = "Hello from EYWA file upload test!\nThis is a test file.\n"
            temp_file.write(test_content)
            temp_file.close()

            file_uuid = TEST_TEXT_FILE_UUID
            self.track_resource("file", file_uuid, "test-file.txt")

            # Upload file
            await upload(
                temp_file.name,
                {
                    "euuid": file_uuid,
                    "name": "test-file.txt",
                    "folder": {"euuid": folder_uuid},
                },
            )

            self.assert_test(True, "Text file uploaded")

            # Verify with GraphQL
            verification = await eywa.graphql(
                """
                query GetFile($uuid: UUID!) {
                    getFile(euuid: $uuid) {
                        euuid
                        name
                        status
                        size
                        content_type
                    }
                }
            """,
                {"uuid": file_uuid},
            )

            file_info = verification.get("getFile")
            self.assert_test(
                file_info is not None and file_info.get("name") == "test-file.txt",
                "Text file verified via GraphQL",
            )

            # Download and verify content
            downloaded_content = await download(file_uuid)
            downloaded_text = downloaded_content.decode("utf-8")

            self.assert_test(
                downloaded_text == test_content,
                "Downloaded content matches original",
                f"Expected: {test_content}, Got: {downloaded_text}",
            )

            return file_uuid

        except Exception as e:
            self.assert_test(False, "Text file upload", str(e))
            return None

        finally:
            if temp_file and os.path.exists(temp_file.name):
                os.unlink(temp_file.name)

    async def test_upload_json_content(self, folder_uuid: str):
        """Test 3: Upload JSON content directly"""
        eywa.info("\n📝 TEST 3: Uploading JSON content...")

        try:
            file_uuid = TEST_JSON_FILE_UUID
            self.track_resource("file", file_uuid, "test-data.json")

            test_data = {
                "message": "Test JSON data",
                "test_id": file_uuid,
                "numbers": [1, 2, 3, 4, 5],
                "nested": {"key": "value"},
            }

            content = json.dumps(test_data, indent=2)

            # Upload JSON content
            await upload_content(
                content,
                {
                    "euuid": file_uuid,
                    "name": "test-data.json",
                    "content_type": "application/json",
                    "folder": {"euuid": folder_uuid},
                },
            )

            self.assert_test(True, "JSON content uploaded")

            # Verify with GraphQL
            verification = await eywa.graphql(
                """
                query GetFile($uuid: UUID!) {
                    getFile(euuid: $uuid) {
                        euuid
                        name
                        content_type
                    }
                }
            """,
                {"uuid": file_uuid},
            )

            file_info = verification.get("getFile")
            self.assert_test(
                file_info is not None
                and file_info.get("content_type") == "application/json",
                "JSON file verified via GraphQL",
            )

            # Download and verify content
            downloaded_content = await download(file_uuid)
            downloaded_json = json.loads(downloaded_content.decode("utf-8"))

            self.assert_test(
                downloaded_json == test_data,
                "Downloaded JSON matches original",
                f"Expected: {test_data}, Got: {downloaded_json}",
            )

            return file_uuid

        except Exception as e:
            self.assert_test(False, "JSON content upload", str(e))
            return None

    async def test_download_to_file(self, file_uuid: str):
        """Test 4: Download file to disk"""
        eywa.info("\n📥 TEST 4: Downloading file to disk...")

        temp_dir = None
        try:
            # Create temp directory
            temp_dir = tempfile.mkdtemp()
            save_path = Path(temp_dir) / "downloaded_test_file.txt"

            # Download to file
            saved_path = await download(file_uuid, save_path)

            self.assert_test(
                os.path.exists(saved_path), "File downloaded to disk successfully"
            )

            # Verify file content
            with open(saved_path, "r") as f:
                content = f.read()

            self.assert_test(
                len(content) > 0,
                "Downloaded file has content",
                f"File size: {len(content)} bytes",
            )

            return True

        except Exception as e:
            self.assert_test(False, "Download to file", str(e))
            return False

        finally:
            if temp_dir and os.path.exists(temp_dir):
                # Clean up temp files
                for file in Path(temp_dir).glob("*"):
                    os.unlink(file)
                os.rmdir(temp_dir)

    async def test_error_handling(self):
        """Test 5: Error handling"""
        eywa.info("\n⚠️  TEST 5: Testing error handling...")

        # Test 5a: Download non-existent file
        fake_uuid = str(uuid.uuid4())
        try:
            await download(fake_uuid)
            self.assert_test(False, "Non-existent file download should fail")
        except FileDownloadError:
            self.assert_test(True, "Correctly caught FileDownloadError for non-existent file")
        except Exception as e:
            self.assert_test(
                False,
                "Non-existent file error handling",
                f"Expected FileDownloadError, got {type(e).__name__}",
            )

        # Test 5b: Upload non-existent file
        try:
            await upload(
                "/tmp/this_file_does_not_exist_12345.txt",
                {"name": "test.txt", "euuid": str(uuid.uuid4())},
            )
            self.assert_test(False, "Non-existent file upload should fail")
        except FileUploadError:
            self.assert_test(True, "Correctly caught FileUploadError for non-existent file")
        except Exception as e:
            self.assert_test(
                False,
                "Non-existent file upload error handling",
                f"Expected FileUploadError, got {type(e).__name__}",
            )

    async def run_all_tests(self):
        """Run all file upload tests"""
        eywa.info("🚀 EYWA File Upload Test Suite")
        eywa.info("=" * 60)

        try:
            # Test 1: Create folder
            folder_uuid = await self.test_create_folder()
            if not folder_uuid:
                eywa.error("Folder creation failed, skipping remaining tests")
                return

            # Test 2: Upload text file
            text_file_uuid = await self.test_upload_text_file(folder_uuid)

            # Test 3: Upload JSON content
            json_file_uuid = await self.test_upload_json_content(folder_uuid)

            # Test 4: Download to file (using text file)
            if text_file_uuid:
                await self.test_download_to_file(text_file_uuid)

            # Test 5: Error handling
            await self.test_error_handling()

            # Print summary
            eywa.info("\n" + "=" * 60)
            eywa.info("📊 TEST SUMMARY")
            eywa.info(f"✅ Passed: {self.test_passed}")
            eywa.info(f"❌ Failed: {self.test_failed}")
            eywa.info(f"📈 Total: {self.test_passed + self.test_failed}")

            if self.test_failed == 0:
                eywa.info("\n🎉 All tests passed!")
                return True
            else:
                eywa.error(f"\n⚠️  {self.test_failed} test(s) failed")
                return False

        except Exception as e:
            eywa.error(f"💥 Test suite failed: {e}")
            import traceback

            eywa.error(traceback.format_exc())
            return False

        finally:
            # Always clean up
            await self.cleanup()


async def main():
    """Main entry point"""
    eywa.open_pipe()

    try:
        test_suite = FileUploadTest()
        success = await test_suite.run_all_tests()

        if success:
            eywa.close_task(eywa.SUCCESS)
        else:
            eywa.close_task(eywa.ERROR)

    except Exception as e:
        eywa.error(f"💥 Test execution failed: {e}")
        eywa.close_task(eywa.ERROR)


if __name__ == "__main__":
    asyncio.run(main())
