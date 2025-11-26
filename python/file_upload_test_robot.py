#!/usr/bin/env python3
"""
EYWA File Upload Test Robot

Tests the eywa-client library file upload/download functionality.
Can be run as an EYWA robot task to verify file operations are working.

Usage:
- Run via EYWA interface as a robot task
- Or manually: eywa run -c "python python/file_upload_test_robot.py"

Input parameters (optional):
- cleanup: boolean (default: true) - whether to cleanup test files after
- max_file_size: int (default: 1000) - max size of test files in bytes
- test_folder_operations: boolean (default: true) - test folder creation
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
from datetime import datetime


class FileUploadTestRobot:
    def __init__(self, config=None):
        self.config = config or {}
        self.test_resources = []
        self.test_passed = 0
        self.test_failed = 0
        self.test_results = []
        self.cleanup_enabled = self.config.get("cleanup", True)

    def track_resource(self, resource_type: str, resource_uuid: str, name: str):
        """Track resource for cleanup"""
        self.test_resources.append(
            {"type": resource_type, "uuid": resource_uuid, "name": name}
        )

    async def cleanup(self):
        """Clean up all test resources"""
        if not self.cleanup_enabled:
            eywa.info("🔒 Cleanup disabled - leaving test files in place")
            return

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

    def record_test(self, test_name: str, passed: bool, details: str = ""):
        """Record test result"""
        result = {
            "test": test_name,
            "passed": passed,
            "details": details,
            "timestamp": datetime.now().isoformat(),
        }
        self.test_results.append(result)

        if passed:
            self.test_passed += 1
            eywa.info(f"✅ PASS: {test_name}")
        else:
            self.test_failed += 1
            eywa.error(f"❌ FAIL: {test_name}" + (f" - {details}" if details else ""))

        return passed

    async def test_create_folder(self):
        """Test: Create a test folder"""
        test_name = "Create folder"
        eywa.info(f"\n📁 TEST: {test_name}")

        try:
            folder_uuid = str(uuid.uuid4())
            self.track_resource("folder", folder_uuid, "test-upload-folder")

            folder = await create_folder(
                {
                    "euuid": folder_uuid,
                    "name": "test-upload-folder",
                    "parent": {"euuid": ROOT_UUID},
                }
            )

            success = folder is not None and folder.get("euuid") == folder_uuid
            self.record_test(test_name, success, f"Folder UUID: {folder_uuid}")

            if success:
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

                folder_info = verification.get("getFolder")
                verified = folder_info is not None
                self.record_test(
                    "Verify folder via GraphQL",
                    verified,
                    f"Path: {folder_info.get('path') if folder_info else 'N/A'}",
                )

            return folder_uuid if success else None

        except Exception as e:
            self.record_test(test_name, False, str(e))
            return None

    async def test_upload_text_file(self, folder_uuid: str):
        """Test: Upload a text file"""
        test_name = "Upload text file"
        eywa.info(f"\n📤 TEST: {test_name}")

        temp_file = None
        try:
            # Create temporary text file
            max_size = self.config.get("max_file_size", 1000)
            temp_file = tempfile.NamedTemporaryFile(
                mode="w", suffix=".txt", delete=False
            )
            test_content = f"EYWA File Upload Test\nTimestamp: {datetime.now().isoformat()}\n"
            test_content += "Test data: " + ("x" * min(max_size - len(test_content), 500))
            temp_file.write(test_content)
            temp_file.close()

            file_uuid = str(uuid.uuid4())
            self.track_resource("file", file_uuid, "test-file.txt")

            # Upload file
            await upload(
                temp_file.name,
                {
                    "euuid": file_uuid,
                    "name": "test-file.txt",
                    "folder": {"euuid": folder_uuid} if folder_uuid else None,
                },
            )

            self.record_test(test_name, True, f"File UUID: {file_uuid}")

            # Verify and download
            downloaded_content = await download(file_uuid)
            downloaded_text = downloaded_content.decode("utf-8")

            content_matches = downloaded_text == test_content
            self.record_test(
                "Verify downloaded content",
                content_matches,
                f"Size: {len(downloaded_content)} bytes",
            )

            return file_uuid

        except Exception as e:
            self.record_test(test_name, False, str(e))
            return None

        finally:
            if temp_file and os.path.exists(temp_file.name):
                os.unlink(temp_file.name)

    async def test_upload_json_content(self, folder_uuid: str):
        """Test: Upload JSON content directly"""
        test_name = "Upload JSON content"
        eywa.info(f"\n📝 TEST: {test_name}")

        try:
            file_uuid = str(uuid.uuid4())
            self.track_resource("file", file_uuid, "test-data.json")

            test_data = {
                "test_type": "file_upload_robot",
                "timestamp": datetime.now().isoformat(),
                "test_id": file_uuid,
                "config": self.config,
            }

            content = json.dumps(test_data, indent=2)

            # Upload JSON content
            await upload_content(
                content,
                {
                    "euuid": file_uuid,
                    "name": "test-data.json",
                    "content_type": "application/json",
                    "folder": {"euuid": folder_uuid} if folder_uuid else None,
                },
            )

            self.record_test(test_name, True, f"File UUID: {file_uuid}")

            # Download and verify
            downloaded_content = await download(file_uuid)
            downloaded_json = json.loads(downloaded_content.decode("utf-8"))

            matches = downloaded_json == test_data
            self.record_test("Verify JSON integrity", matches, "Content matches original")

            return file_uuid

        except Exception as e:
            self.record_test(test_name, False, str(e))
            return None

    async def test_error_handling(self):
        """Test: Error handling"""
        eywa.info("\n⚠️  TEST: Error handling")

        # Test 1: Download non-existent file
        try:
            fake_uuid = str(uuid.uuid4())
            await download(fake_uuid)
            self.record_test("Error: Non-existent download", False, "Should have raised error")
        except FileDownloadError:
            self.record_test("Error: Non-existent download", True, "Correctly caught FileDownloadError")
        except Exception as e:
            self.record_test("Error: Non-existent download", False, f"Wrong exception: {type(e).__name__}")

        # Test 2: Upload non-existent file
        try:
            await upload(
                "/tmp/this_file_does_not_exist_eywa_test.txt",
                {"name": "test.txt", "euuid": str(uuid.uuid4())},
            )
            self.record_test("Error: Non-existent upload", False, "Should have raised error")
        except FileUploadError:
            self.record_test("Error: Non-existent upload", True, "Correctly caught FileUploadError")
        except Exception as e:
            self.record_test("Error: Non-existent upload", False, f"Wrong exception: {type(e).__name__}")

    async def run_tests(self):
        """Run all file upload tests"""
        eywa.info("🚀 Starting File Upload Test Robot")
        eywa.info(f"Configuration: {json.dumps(self.config, indent=2)}")
        eywa.update_task(eywa.PROCESSING)

        start_time = datetime.now()

        try:
            # Test 1: Create folder (if enabled)
            folder_uuid = None
            if self.config.get("test_folder_operations", True):
                folder_uuid = await self.test_create_folder()

            # Test 2: Upload text file
            text_file_uuid = await self.test_upload_text_file(folder_uuid)

            # Test 3: Upload JSON content
            json_file_uuid = await self.test_upload_json_content(folder_uuid)

            # Test 4: Error handling
            await self.test_error_handling()

            # Calculate duration
            duration = (datetime.now() - start_time).total_seconds()

            # Create report
            report = {
                "summary": {
                    "total_tests": self.test_passed + self.test_failed,
                    "passed": self.test_passed,
                    "failed": self.test_failed,
                    "success_rate": (
                        f"{(self.test_passed / (self.test_passed + self.test_failed) * 100):.1f}%"
                        if (self.test_passed + self.test_failed) > 0
                        else "0%"
                    ),
                    "duration_seconds": round(duration, 2),
                },
                "config": self.config,
                "test_results": self.test_results,
                "resources_created": len(self.test_resources),
                "cleanup_enabled": self.cleanup_enabled,
                "timestamp": datetime.now().isoformat(),
            }

            # Log summary
            eywa.info("\n" + "=" * 60)
            eywa.info("📊 TEST SUMMARY")
            eywa.info(f"✅ Passed: {self.test_passed}")
            eywa.info(f"❌ Failed: {self.test_failed}")
            eywa.info(f"📈 Total: {self.test_passed + self.test_failed}")
            eywa.info(f"⏱️  Duration: {duration:.2f}s")

            # Create message for report
            if self.test_failed == 0:
                message = f"✅ All {self.test_passed} file upload tests passed successfully!"
            else:
                message = f"⚠️ File upload tests completed: {self.test_passed} passed, {self.test_failed} failed"

            eywa.report(message, report)

            return self.test_failed == 0

        except Exception as e:
            eywa.error(f"💥 Test suite failed: {e}")
            import traceback

            eywa.error(traceback.format_exc())
            return False

        finally:
            # Always clean up
            await self.cleanup()


async def main():
    """Main robot entry point"""
    eywa.open_pipe()

    try:
        # Get task input
        task = await eywa.get_task()
        input_data = task.get("data", {})

        eywa.info(f"Received task configuration: {input_data}")

        # Create and run test robot
        robot = FileUploadTestRobot(config=input_data)
        success = await robot.run_tests()

        if success:
            eywa.info("🎉 All tests passed!")
            eywa.close_task(eywa.SUCCESS)
        else:
            eywa.error("⚠️  Some tests failed")
            eywa.close_task(eywa.ERROR)

    except Exception as e:
        eywa.error(f"💥 Robot execution failed: {e}")
        import traceback

        eywa.error(traceback.format_exc())
        eywa.close_task(eywa.ERROR)


if __name__ == "__main__":
    asyncio.run(main())
