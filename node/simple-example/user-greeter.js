import eywa from 'eywa-client'

/**
 * User Greeter Robot
 * 
 * This robot demonstrates basic GraphQL operations:
 * - Reading data (query)
 * - Creating/updating data (mutation)
 */

async function main() {
    eywa.open_pipe()
    
    try {
        const task = await eywa.get_task()
        const { userId, greeting } = task.input
        
        eywa.info('Starting user greeting task', { userId, greeting })
        eywa.update_task(eywa.PROCESSING)
        
        // Step 1: Query to find the user
        const searchResult = await eywa.graphql(`
            query findUser($euuid: ID!) {
                User(euuid: $euuid) {
                    euuid
                    name
                    email
                    last_greeted
                }
            }
        `, { euuid: userId })
        
        const user = searchResult.data.User
        
        if (!user) {
            throw new Error(`User not found: ${userId}`)
        }
        
        eywa.info('Found user', { name: user.name, email: user.email })
        
        // Step 2: Update the user with greeting info
        const customGreeting = greeting || `Hello ${user.name}! Welcome back!`
        
        const updateResult = await eywa.graphql(`
            mutation updateUserGreeting($euuid: ID!, $data: UserInput!) {
                syncUser(euuid: $euuid, data: $data) {
                    euuid
                    last_greeted
                    greeting_count
                }
            }
        `, {
            euuid: userId,
            data: {
                last_greeted: new Date().toISOString(),
                greeting_count: (user.greeting_count || 0) + 1
            }
        })
        
        // Step 3: Report results
        const result = {
            user: {
                euuid: user.euuid,
                name: user.name,
                email: user.email
            },
            greeting: customGreeting,
            previouslyGreeted: user.last_greeted,
            greetingCount: updateResult.data.syncUser.greeting_count
        }
        
        eywa.report('User greeted successfully', result)
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        eywa.error('Failed to greet user', { 
            error: error.message 
        })
        eywa.close_task(eywa.ERROR)
    }
}

main()
