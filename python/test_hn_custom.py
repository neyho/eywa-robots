#!/usr/bin/env python3
"""Test HN scraper with custom patterns"""

import json
import asyncio

# Mock the eywa module for testing
class MockEywa:
    PROCESSING = "PROCESSING"
    SUCCESS = "SUCCESS"
    ERROR = "ERROR"
    
    @staticmethod
    def open_pipe(): pass
    
    @staticmethod
    def info(msg, data=None): print(f"[INFO] {msg}")
    
    @staticmethod
    def warn(msg): print(f"[WARN] {msg}")
    
    @staticmethod
    def error(msg): print(f"[ERROR] {msg}")
    
    @staticmethod
    def report(msg, data): 
        print(f"\n[REPORT] {msg}")
        print(json.dumps(data, indent=2))
    
    @staticmethod
    def update_task(status): pass
    
    @staticmethod
    def close_task(status): pass
    
    @staticmethod
    async def get_task():
        # You can modify this to test different inputs
        return {
            'input': {
                'search_term': 'robotics',
                'regex_pattern': r'robot|robotic|RPA|automation',
                'max_results': 5,
                'check_comments': False
            }
        }
    
    @staticmethod
    async def graphql(query, variables=None):
        return {}

# Replace the import
import sys
sys.modules['eywa'] = MockEywa

# Now import the actual scraper
from hn_scraper import main

# Run it
if __name__ == "__main__":
    print("🚀 Testing HN Scraper with custom search...")
    print("-" * 60)
    asyncio.run(main())
