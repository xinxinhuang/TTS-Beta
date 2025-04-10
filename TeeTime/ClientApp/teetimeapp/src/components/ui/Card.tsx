import React from 'react'

type CardProps = {
  children: React.ReactNode
  title?: string
  subtitle?: string
  className?: string
}

export default function Card({ children, title, subtitle, className = '' }: CardProps) {
  return (
    <div className={`bg-white rounded-lg shadow-md overflow-hidden ${className}`}>
      {(title || subtitle) && (
        <div className="px-6 py-4 border-b border-gray-200">
          {title && <h3 className="text-xl font-semibold text-gray-800">{title}</h3>}
          {subtitle && <p className="mt-1 text-gray-600">{subtitle}</p>}
        </div>
      )}
      <div className="p-6">
        {children}
      </div>
    </div>
  )
} 