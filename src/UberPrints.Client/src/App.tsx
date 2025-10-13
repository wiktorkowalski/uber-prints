import { useState } from 'react'

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className="min-h-screen bg-background flex items-center justify-center">
      <div className="container mx-auto p-4">
        <div className="max-w-2xl mx-auto text-center space-y-8">
          <h1 className="text-4xl font-bold text-foreground">
            UberPrints 3D Request System
          </h1>

          <div className="p-8 bg-card rounded-lg border shadow-sm">
            <p className="text-lg text-muted-foreground mb-6">
              Welcome to the UberPrints client application. The setup is complete!
            </p>

            <div className="space-y-4">
              <button
                className="px-6 py-3 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors"
                onClick={() => setCount((count) => count + 1)}
              >
                Count is {count}
              </button>

              <p className="text-sm text-muted-foreground">
                This is a placeholder page. Run <code className="bg-muted px-2 py-1 rounded">npm install</code> in the Client directory to get started.
              </p>
            </div>
          </div>

          <div className="text-sm text-muted-foreground">
            <p>Built with React 18 + TypeScript + Vite + shadcn/ui</p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default App
