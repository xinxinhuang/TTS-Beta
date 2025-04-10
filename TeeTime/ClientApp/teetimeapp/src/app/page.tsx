import Card from '@/components/ui/Card'
import Button from '@/components/ui/Button'

export default function Home() {
  return (
    <div className="container mx-auto px-4 py-8">
      <header className="mb-8">
        <h1 className="text-4xl font-bold text-blue-600">TeeTime Management System</h1>
        <p className="text-lg text-gray-600 mt-2">Schedule and manage your golf tee times with ease</p>
      </header>
      
      <section className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <Card 
          title="Schedule Tee Time" 
          subtitle="Book your next golf session with just a few clicks."
        >
          <Button variant="success">Book Now</Button>
        </Card>
        
        <Card 
          title="View Schedule" 
          subtitle="Check your upcoming tee times and reservations."
        >
          <Button>View Calendar</Button>
        </Card>
        
        <Card 
          title="Manage Standing Times" 
          subtitle="Set up and manage your regular standing tee times."
        >
          <div className="space-y-3">
            <p className="text-gray-700">Manage your recurring tee times with our easy scheduling system.</p>
            <Button variant="secondary" fullWidth>Manage Now</Button>
          </div>
        </Card>
      </section>
    </div>
  )
} 